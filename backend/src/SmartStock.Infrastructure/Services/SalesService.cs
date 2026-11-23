using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class SalesService : ISalesService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SalesService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<SaleDto>> CreateSaleAsync(CreateSaleDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ValidationAppException("Items", "At least one sale item is required.");
        }

        // Use ExecutionStrategy to support transactions with retries and concurrency safety
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
            try
            {
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();

                // Load products from DB belonging to this business
                var products = await _context.Products
                    .Where(p => p.BusinessId == businessId && productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                // 1. Validate all products belong to the authenticated business
                foreach (var reqItem in dto.Items)
                {
                    if (!products.TryGetValue(reqItem.ProductId, out var product))
                    {
                        throw new ValidationAppException("ProductId", $"Product with ID '{reqItem.ProductId}' not found in your business catalog.");
                    }

                    if (!product.IsActive)
                    {
                        throw new ValidationAppException("ProductId", $"Product '{product.Name}' is inactive.");
                    }

                    if (reqItem.Quantity <= 0)
                    {
                        throw new ValidationAppException("Quantity", $"Quantity for '{product.Name}' must be greater than zero.");
                    }
                }

                // Aggregate requested quantities per product to avoid double counting if duplicate items submitted
                var aggregatedRequests = dto.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                // 2 & 3. Check product availability and prevent negative stock
                foreach (var (productId, totalRequestedQty) in aggregatedRequests)
                {
                    var product = products[productId];
                    if (product.CurrentStock < totalRequestedQty)
                    {
                        throw new InsufficientStockException(product.Name, totalRequestedQty, product.CurrentStock);
                    }
                }

                // Generate Unique Invoice Number: INV-YYYYMMDD-XXXX
                var dateStr = dto.SaleDate.ToString("yyyyMMdd");
                var countToday = await _context.Sales
                    .CountAsync(s => s.BusinessId == businessId && s.InvoiceNumber.StartsWith($"INV-{dateStr}"), cancellationToken);
                var invoiceNumber = $"INV-{dateStr}-{(countToday + 1):D4}";

                var sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    InvoiceNumber = invoiceNumber,
                    SaleDate = dto.SaleDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.SaleDate, DateTimeKind.Utc),
                    Notes = dto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                decimal calculatedTotal = 0;
                var saleItems = new List<SaleItem>();
                var stockTransactions = new List<StockTransaction>();

                // 4, 5, 6, 7, 8. Calculate line totals on backend, reduce stock, create transactions
                foreach (var reqItem in dto.Items)
                {
                    var product = products[reqItem.ProductId];

                    // Unit price: Use backend selling price if frontend provided <= 0, else validate
                    var effectiveUnitPrice = reqItem.UnitPrice > 0 ? reqItem.UnitPrice : product.SellingPrice;
                    var lineTotal = Math.Round(reqItem.Quantity * effectiveUnitPrice, 2);

                    calculatedTotal += lineTotal;

                    var saleItem = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        SaleId = sale.Id,
                        ProductId = product.Id,
                        Quantity = reqItem.Quantity,
                        UnitPrice = effectiveUnitPrice,
                        SubTotal = lineTotal,
                        Product = product
                    };
                    saleItems.Add(saleItem);

                    var prevStock = product.CurrentStock;
                    var newStock = prevStock - reqItem.Quantity;

                    // Reduce stock
                    product.CurrentStock = newStock;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Create StockTransaction
                    var stockTx = new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        TransactionType = TransactionType.Sale,
                        Quantity = reqItem.Quantity,
                        PreviousStock = prevStock,
                        NewStock = newStock,
                        ReferenceId = sale.Id,
                        Notes = $"Sale Invoice {invoiceNumber}",
                        CreatedAt = DateTime.UtcNow
                    };
                    stockTransactions.Add(stockTx);

                    // Check low stock condition and create notification if newly triggered
                    if (newStock <= product.MinimumStock)
                    {
                        var isOutOfStock = newStock <= 0;
                        var hasUnreadNotification = await _context.Notifications.AnyAsync(
                            n => n.BusinessId == businessId && n.ProductId == product.Id && !n.IsRead,
                            cancellationToken);

                        if (!hasUnreadNotification)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                Id = Guid.NewGuid(),
                                BusinessId = businessId,
                                ProductId = product.Id,
                                Title = isOutOfStock ? $"Out of Stock: {product.Name}" : $"Low Stock: {product.Name}",
                                Message = isOutOfStock
                                    ? $"{product.Name} (SKU: {product.SKU}) is completely out of stock."
                                    : $"{product.Name} (SKU: {product.SKU}) stock ({newStock} {product.Unit}) has reached or fallen below minimum threshold ({product.MinimumStock}).",
                                NotificationType = isOutOfStock ? NotificationType.OutOfStock : NotificationType.LowStock,
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                sale.TotalAmount = calculatedTotal;
                sale.Items = saleItems;

                _context.Sales.Add(sale);
                _context.StockTransactions.AddRange(stockTransactions);

                // 9. Commit all changes atomically in a single database transaction
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // 10. Return invoice details
                return ApiResponse<SaleDto>.Ok(MapToDto(sale), "Sale recorded and inventory updated successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<ApiResponse<PagedResult<SaleDto>>> GetSalesAsync(
        PaginationParams pagination,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Where(s => s.BusinessId == businessId);

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(s => s.SaleDate <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var search = pagination.Search.Trim().ToLower();
            query = query.Where(s => s.InvoiceNumber.ToLower().Contains(search) || (s.Notes != null && s.Notes.ToLower().Contains(search)));
        }

        query = pagination.SortBy?.ToLower() switch
        {
            "amount" or "totalamount" => pagination.SortDescending ? query.OrderByDescending(s => s.TotalAmount) : query.OrderBy(s => s.TotalAmount),
            "date" or "saledate" => pagination.SortDescending ? query.OrderByDescending(s => s.SaleDate) : query.OrderBy(s => s.SaleDate),
            _ => pagination.SortDescending ? query.OrderByDescending(s => s.SaleDate) : query.OrderByDescending(s => s.SaleDate)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<SaleDto>>.Ok(
            new PagedResult<SaleDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Sales retrieved successfully.");
    }

    public async Task<ApiResponse<SaleDto>> GetSaleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (sale == null)
        {
            throw new NotFoundException("Sale", id);
        }

        return ApiResponse<SaleDto>.Ok(MapToDto(sale), "Sale details retrieved successfully.");
    }

    public async Task<ApiResponse<SalesSummaryDto>> GetSalesSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var todaySales = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= today)
            .ToListAsync(cancellationToken);

        var monthSales = await _context.Sales
            .Where(s => s.BusinessId == businessId && s.SaleDate >= startOfMonth)
            .ToListAsync(cancellationToken);

        var allTimeSalesTotal = await _context.Sales
            .Where(s => s.BusinessId == businessId)
            .SumAsync(s => (decimal?)s.TotalAmount, cancellationToken) ?? 0;

        var summary = new SalesSummaryDto
        {
            TotalSalesToday = todaySales.Sum(s => s.TotalAmount),
            TotalSalesThisMonth = monthSales.Sum(s => s.TotalAmount),
            TotalSalesAllTime = allTimeSalesTotal,
            TotalOrdersToday = todaySales.Count,
            TotalOrdersThisMonth = monthSales.Count
        };

        return ApiResponse<SalesSummaryDto>.Ok(summary, "Sales summary retrieved successfully.");
    }

    private static SaleDto MapToDto(Sale s)
    {
        return new SaleDto
        {
            Id = s.Id,
            BusinessId = s.BusinessId,
            InvoiceNumber = s.InvoiceNumber,
            TotalAmount = s.TotalAmount,
            SaleDate = s.SaleDate,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            Items = s.Items.Select(i => new SaleItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : string.Empty,
                SKU = i.Product != null ? i.Product.SKU : string.Empty,
                Unit = i.Product != null ? i.Product.Unit : UnitType.Piece,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}
