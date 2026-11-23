using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ValidationAppException("Items", "At least one purchase item is required.");
        }

        // Validate supplier if provided
        Supplier? supplier = null;
        if (dto.SupplierId.HasValue)
        {
            supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId.Value && s.BusinessId == businessId, cancellationToken);

            if (supplier == null)
            {
                throw new ValidationAppException("SupplierId", "The selected supplier does not belong to your store.");
            }
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
            try
            {
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();

                var products = await _context.Products
                    .Where(p => p.BusinessId == businessId && productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                foreach (var reqItem in dto.Items)
                {
                    if (!products.TryGetValue(reqItem.ProductId, out var product))
                    {
                        throw new ValidationAppException("ProductId", $"Product '{reqItem.ProductId}' not found in your store.");
                    }

                    if (reqItem.Quantity <= 0)
                    {
                        throw new ValidationAppException("Quantity", $"Quantity for '{product.Name}' must be greater than zero.");
                    }
                }

                var dateStr = dto.PurchaseDate.ToString("yyyyMMdd");
                var countToday = await _context.Purchases
                    .CountAsync(p => p.BusinessId == businessId && p.InvoiceNumber.StartsWith($"PUR-{dateStr}"), cancellationToken);
                var invoiceNumber = $"PUR-{dateStr}-{(countToday + 1):D4}";

                var purchase = new Purchase
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    SupplierId = dto.SupplierId,
                    Supplier = supplier,
                    InvoiceNumber = invoiceNumber,
                    PurchaseDate = dto.PurchaseDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.PurchaseDate, DateTimeKind.Utc),
                    Notes = dto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                decimal calculatedTotal = 0;
                var purchaseItems = new List<PurchaseItem>();
                var stockTransactions = new List<StockTransaction>();

                foreach (var reqItem in dto.Items)
                {
                    var product = products[reqItem.ProductId];
                    var effectiveUnitCost = reqItem.UnitCost > 0 ? reqItem.UnitCost : product.PurchasePrice;
                    var lineTotal = Math.Round(reqItem.Quantity * effectiveUnitCost, 2);

                    calculatedTotal += lineTotal;

                    var pItem = new PurchaseItem
                    {
                        Id = Guid.NewGuid(),
                        PurchaseId = purchase.Id,
                        ProductId = product.Id,
                        Quantity = reqItem.Quantity,
                        UnitCost = effectiveUnitCost,
                        SubTotal = lineTotal,
                        Product = product
                    };
                    purchaseItems.Add(pItem);

                    // Increase stock
                    var prevStock = product.CurrentStock;
                    var newStock = prevStock + reqItem.Quantity;

                    product.CurrentStock = newStock;
                    // Update purchase price if provided
                    if (reqItem.UnitCost > 0)
                    {
                        product.PurchasePrice = reqItem.UnitCost;
                    }
                    product.UpdatedAt = DateTime.UtcNow;

                    stockTransactions.Add(new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        TransactionType = TransactionType.Purchase,
                        Quantity = reqItem.Quantity,
                        PreviousStock = prevStock,
                        NewStock = newStock,
                        ReferenceId = purchase.Id,
                        Notes = $"Purchase Invoice {invoiceNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                purchase.TotalAmount = calculatedTotal;
                purchase.Items = purchaseItems;

                _context.Purchases.Add(purchase);
                _context.StockTransactions.AddRange(stockTransactions);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ApiResponse<PurchaseDto>.Ok(MapToDto(purchase), "Purchase recorded and inventory updated successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<ApiResponse<PagedResult<PurchaseDto>>> GetPurchasesAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .ThenInclude(i => i.Product)
            .Where(p => p.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var search = pagination.Search.Trim().ToLower();
            query = query.Where(p => p.InvoiceNumber.ToLower().Contains(search) 
                                     || (p.Supplier != null && p.Supplier.SupplierName.ToLower().Contains(search)));
        }

        query = pagination.SortBy?.ToLower() switch
        {
            "amount" or "totalamount" => pagination.SortDescending ? query.OrderByDescending(p => p.TotalAmount) : query.OrderBy(p => p.TotalAmount),
            "date" or "purchasedate" => pagination.SortDescending ? query.OrderByDescending(p => p.PurchaseDate) : query.OrderBy(p => p.PurchaseDate),
            _ => pagination.SortDescending ? query.OrderByDescending(p => p.PurchaseDate) : query.OrderByDescending(p => p.PurchaseDate)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<PurchaseDto>>.Ok(
            new PagedResult<PurchaseDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Purchases retrieved successfully.");
    }

    public async Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var purchase = await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId, cancellationToken);

        if (purchase == null)
        {
            throw new NotFoundException("Purchase", id);
        }

        return ApiResponse<PurchaseDto>.Ok(MapToDto(purchase), "Purchase retrieved successfully.");
    }

    public async Task<ApiResponse<PurchasesSummaryDto>> GetPurchasesSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthPurchases = await _context.Purchases
            .Where(p => p.BusinessId == businessId && p.PurchaseDate >= startOfMonth)
            .ToListAsync(cancellationToken);

        var allTimeTotal = await _context.Purchases
            .Where(p => p.BusinessId == businessId)
            .SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0;

        var summary = new PurchasesSummaryDto
        {
            TotalPurchasesThisMonth = monthPurchases.Sum(p => p.TotalAmount),
            TotalPurchasesAllTime = allTimeTotal,
            TotalOrdersThisMonth = monthPurchases.Count
        };

        return ApiResponse<PurchasesSummaryDto>.Ok(summary, "Purchases summary retrieved successfully.");
    }

    private static PurchaseDto MapToDto(Purchase p)
    {
        return new PurchaseDto
        {
            Id = p.Id,
            BusinessId = p.BusinessId,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier != null ? p.Supplier.SupplierName : null,
            InvoiceNumber = p.InvoiceNumber,
            TotalAmount = p.TotalAmount,
            PurchaseDate = p.PurchaseDate,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            Items = p.Items.Select(i => new PurchaseItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : string.Empty,
                SKU = i.Product != null ? i.Product.SKU : string.Empty,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}
