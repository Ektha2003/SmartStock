using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public InventoryService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<InventorySummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var products = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        var summary = new InventorySummaryDto
        {
            TotalProducts = products.Count,
            TotalStockQuantity = products.Sum(p => p.CurrentStock),
            TotalInventoryCostValue = products.Sum(p => p.CurrentStock * p.PurchasePrice),
            TotalInventoryRetailValue = products.Sum(p => p.CurrentStock * p.SellingPrice),
            LowStockCount = products.Count(p => p.CurrentStock <= p.MinimumStock && p.CurrentStock > 0),
            OutOfStockCount = products.Count(p => p.CurrentStock <= 0)
        };

        return ApiResponse<InventorySummaryDto>.Ok(summary, "Inventory summary retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResult<StockTransactionDto>>> GetTransactionsAsync(
        PaginationParams pagination,
        Guid? productId = null,
        TransactionType? type = null,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.StockTransactions
            .Include(st => st.Product)
            .Where(st => st.BusinessId == businessId);

        if (productId.HasValue)
        {
            query = query.Where(st => st.ProductId == productId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(st => st.TransactionType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var search = pagination.Search.Trim().ToLower();
            query = query.Where(st => st.Product.Name.ToLower().Contains(search) 
                                      || st.Product.SKU.ToLower().Contains(search) 
                                      || (st.Notes != null && st.Notes.ToLower().Contains(search)));
        }

        query = query.OrderByDescending(st => st.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(st => MapToDto(st))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<StockTransactionDto>>.Ok(
            new PagedResult<StockTransactionDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Stock transactions retrieved successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<StockTransactionDto>>> GetProductHistoryAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var exists = await _context.Products.AnyAsync(p => p.Id == productId && p.BusinessId == businessId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Product", productId);
        }

        var history = await _context.StockTransactions
            .Include(st => st.Product)
            .Where(st => st.BusinessId == businessId && st.ProductId == productId)
            .OrderByDescending(st => st.CreatedAt)
            .Select(st => MapToDto(st))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<StockTransactionDto>>.Ok(history, "Product stock history retrieved successfully.");
    }

    public async Task<ApiResponse<StockTransactionDto>> AdjustStockAsync(StockAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new ValidationAppException("Reason", "A valid reason for manual stock adjustment is mandatory for audit compliance.");
        }

        if (dto.NewQuantity < 0)
        {
            throw new ValidationAppException("NewQuantity", "Stock quantity cannot be adjusted to a negative value.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.BusinessId == businessId, cancellationToken);

                if (product == null)
                {
                    throw new NotFoundException("Product", dto.ProductId);
                }

                var previousStock = product.CurrentStock;
                var difference = dto.NewQuantity - previousStock;

                product.CurrentStock = dto.NewQuantity;
                product.UpdatedAt = DateTime.UtcNow;

                var stockTx = new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    ProductId = product.Id,
                    TransactionType = TransactionType.Adjustment,
                    Quantity = Math.Abs(difference),
                    PreviousStock = previousStock,
                    NewStock = dto.NewQuantity,
                    Notes = $"Manual Adjustment: {dto.Reason.Trim()} (Delta: {(difference >= 0 ? "+" : "")}{difference})",
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockTransactions.Add(stockTx);

                // Trigger notification if low stock
                if (dto.NewQuantity <= product.MinimumStock)
                {
                    var isOutOfStock = dto.NewQuantity <= 0;
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
                            Message = $"{product.Name} (SKU: {product.SKU}) stock was manually adjusted to {dto.NewQuantity} {product.Unit}.",
                            NotificationType = isOutOfStock ? NotificationType.OutOfStock : NotificationType.LowStock,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                stockTx.Product = product;
                return ApiResponse<StockTransactionDto>.Ok(MapToDto(stockTx), "Stock adjusted and audited successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static StockTransactionDto MapToDto(StockTransaction st)
    {
        return new StockTransactionDto
        {
            Id = st.Id,
            BusinessId = st.BusinessId,
            ProductId = st.ProductId,
            ProductName = st.Product != null ? st.Product.Name : string.Empty,
            SKU = st.Product != null ? st.Product.SKU : string.Empty,
            TransactionType = st.TransactionType,
            Quantity = st.Quantity,
            PreviousStock = st.PreviousStock,
            NewStock = st.NewStock,
            ReferenceId = st.ReferenceId,
            Notes = st.Notes,
            CreatedAt = st.CreatedAt
        };
    }
}
