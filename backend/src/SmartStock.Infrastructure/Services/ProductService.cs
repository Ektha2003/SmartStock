using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProductService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetPagedAsync(ProductFilterParams filterParams, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId);

        if (filterParams.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filterParams.IsActive.Value);
        }
        else
        {
            // Default: show active products unless specified
            query = query.Where(p => p.IsActive);
        }

        if (filterParams.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filterParams.CategoryId.Value);
        }

        if (filterParams.LowStockOnly.HasValue && filterParams.LowStockOnly.Value)
        {
            query = query.Where(p => p.CurrentStock <= p.MinimumStock);
        }

        if (!string.IsNullOrWhiteSpace(filterParams.Search))
        {
            var search = filterParams.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.SKU.ToLower().Contains(search));
        }

        // Sorting
        query = filterParams.SortBy?.ToLower() switch
        {
            "name" => filterParams.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "stock" or "currentstock" => filterParams.SortDescending ? query.OrderByDescending(p => p.CurrentStock) : query.OrderBy(p => p.CurrentStock),
            "price" or "sellingprice" => filterParams.SortDescending ? query.OrderByDescending(p => p.SellingPrice) : query.OrderBy(p => p.SellingPrice),
            "category" => filterParams.SortDescending ? query.OrderByDescending(p => p.Category.Name) : query.OrderBy(p => p.Category.Name),
            "sku" => filterParams.SortDescending ? query.OrderByDescending(p => p.SKU) : query.OrderBy(p => p.SKU),
            _ => filterParams.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
            .Take(filterParams.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        var pagedResult = new PagedResult<ProductDto>(items, totalCount, filterParams.PageNumber, filterParams.PageSize);
        return ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult, "Products retrieved successfully.");
    }

    public async Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        return ApiResponse<ProductDto>.Ok(MapToDto(product), "Product retrieved successfully.");
    }

    public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var skuTrimmed = dto.SKU.Trim().ToUpperInvariant();

        // 1. Validate Category belongs to this business
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.BusinessId == businessId, cancellationToken);

        if (category == null)
        {
            throw new ValidationAppException("CategoryId", "The selected category does not belong to your store or does not exist.");
        }

        // 2. Validate SKU uniqueness within the business
        var skuExists = await _context.Products
            .AnyAsync(p => p.BusinessId == businessId && p.SKU.ToUpper() == skuTrimmed, cancellationToken);

        if (skuExists)
        {
            throw new ConflictAppException($"A product with SKU '{skuTrimmed}' already exists in your store.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    CategoryId = dto.CategoryId,
                    Name = dto.Name.Trim(),
                    SKU = skuTrimmed,
                    Description = dto.Description?.Trim() ?? string.Empty,
                    PurchasePrice = dto.PurchasePrice,
                    SellingPrice = dto.SellingPrice,
                    CurrentStock = dto.CurrentStock,
                    MinimumStock = dto.MinimumStock,
                    Unit = dto.Unit,
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                // Audit initial stock with StockTransaction
                if (dto.CurrentStock > 0)
                {
                    _context.StockTransactions.Add(new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        TransactionType = TransactionType.Opening,
                        Quantity = dto.CurrentStock,
                        PreviousStock = 0,
                        NewStock = dto.CurrentStock,
                        Notes = "Initial opening stock recorded at product creation",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                product.Category = category;
                return ApiResponse<ProductDto>.Ok(MapToDto(product), "Product created successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var skuTrimmed = dto.SKU.Trim().ToUpperInvariant();

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        // Validate Category
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.BusinessId == businessId, cancellationToken);

        if (category == null)
        {
            throw new ValidationAppException("CategoryId", "The selected category does not belong to your store.");
        }

        // Validate SKU uniqueness
        var skuExists = await _context.Products
            .AnyAsync(p => p.BusinessId == businessId && p.Id != id && p.SKU.ToUpper() == skuTrimmed, cancellationToken);

        if (skuExists)
        {
            throw new ConflictAppException($"Another product with SKU '{skuTrimmed}' already exists.");
        }

        product.Name = dto.Name.Trim();
        product.SKU = skuTrimmed;
        product.CategoryId = dto.CategoryId;
        product.Description = dto.Description?.Trim() ?? string.Empty;
        product.PurchasePrice = dto.PurchasePrice;
        product.SellingPrice = dto.SellingPrice;
        product.MinimumStock = dto.MinimumStock;
        product.Unit = dto.Unit;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        product.Category = category;
        return ApiResponse<ProductDto>.Ok(MapToDto(product), "Product updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        // Soft delete
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product deactivated successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<ProductDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId && p.IsActive && p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<ProductDto>>.Ok(products, "Low stock products retrieved successfully.");
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            BusinessId = p.BusinessId,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : string.Empty,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            PurchasePrice = p.PurchasePrice,
            SellingPrice = p.SellingPrice,
            CurrentStock = p.CurrentStock,
            MinimumStock = p.MinimumStock,
            Unit = p.Unit,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
