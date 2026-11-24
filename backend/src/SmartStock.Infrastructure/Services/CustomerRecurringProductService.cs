using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class CustomerRecurringProductService : ICustomerRecurringProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CustomerRecurringProductService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<IReadOnlyList<CustomerRecurringProductDto>>> GetProductsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

        if (!customerExists)
        {
            throw new NotFoundAppException("Customer", customerId);
        }

        var items = await _context.CustomerRecurringProducts
            .Include(rp => rp.Product)
            .Where(rp => rp.CustomerId == customerId && rp.BusinessId == businessId)
            .OrderBy(rp => rp.Product.Name)
            .Select(rp => new CustomerRecurringProductDto
            {
                Id = rp.Id,
                CustomerId = rp.CustomerId,
                ProductId = rp.ProductId,
                ProductName = rp.Product.Name,
                ProductSku = rp.Product.SKU,
                DefaultQuantity = rp.DefaultQuantity,
                Unit = rp.Unit,
                PriceOverride = rp.PriceOverride,
                EffectivePrice = rp.PriceOverride ?? rp.Product.SellingPrice,
                MinQuantity = rp.MinQuantity,
                MaxQuantity = rp.MaxQuantity,
                ProductInstructions = rp.ProductInstructions,
                IsActive = rp.IsActive,
                CurrentStock = rp.Product.CurrentStock
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<CustomerRecurringProductDto>>.Ok(items, "Customer recurring products retrieved.");
    }

    public async Task<ApiResponse<CustomerRecurringProductDto>> AddProductAsync(Guid customerId, CreateCustomerRecurringProductDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", customerId);
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.BusinessId == businessId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundAppException("Product", dto.ProductId);
        }

        var alreadyExists = await _context.CustomerRecurringProducts
            .AnyAsync(rp => rp.CustomerId == customerId && rp.ProductId == dto.ProductId && rp.BusinessId == businessId, cancellationToken);

        if (alreadyExists)
        {
            throw new ValidationAppException("ProductId", $"Product '{product.Name}' is already configured in this customer's recurring list.");
        }

        var recurringProduct = new CustomerRecurringProduct
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customerId,
            ProductId = dto.ProductId,
            DefaultQuantity = dto.DefaultQuantity,
            Unit = dto.Unit,
            PriceOverride = dto.PriceOverride,
            MinQuantity = dto.MinQuantity,
            MaxQuantity = dto.MaxQuantity,
            ProductInstructions = dto.ProductInstructions?.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.CustomerRecurringProducts.Add(recurringProduct);
        await _context.SaveChangesAsync(cancellationToken);

        var result = new CustomerRecurringProductDto
        {
            Id = recurringProduct.Id,
            CustomerId = recurringProduct.CustomerId,
            ProductId = recurringProduct.ProductId,
            ProductName = product.Name,
            ProductSku = product.SKU,
            DefaultQuantity = recurringProduct.DefaultQuantity,
            Unit = recurringProduct.Unit,
            PriceOverride = recurringProduct.PriceOverride,
            EffectivePrice = recurringProduct.PriceOverride ?? product.SellingPrice,
            MinQuantity = recurringProduct.MinQuantity,
            MaxQuantity = recurringProduct.MaxQuantity,
            ProductInstructions = recurringProduct.ProductInstructions,
            IsActive = recurringProduct.IsActive,
            CurrentStock = product.CurrentStock
        };

        return ApiResponse<CustomerRecurringProductDto>.Ok(result, "Product added to customer recurring list.");
    }

    public async Task<ApiResponse<CustomerRecurringProductDto>> UpdateProductAsync(Guid customerId, Guid recurringProductId, UpdateCustomerRecurringProductDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var recurringProduct = await _context.CustomerRecurringProducts
            .Include(rp => rp.Product)
            .FirstOrDefaultAsync(rp => rp.Id == recurringProductId && rp.CustomerId == customerId && rp.BusinessId == businessId, cancellationToken);

        if (recurringProduct == null)
        {
            throw new NotFoundAppException("CustomerRecurringProduct", recurringProductId);
        }

        recurringProduct.DefaultQuantity = dto.DefaultQuantity;
        recurringProduct.Unit = dto.Unit;
        recurringProduct.PriceOverride = dto.PriceOverride;
        recurringProduct.MinQuantity = dto.MinQuantity;
        recurringProduct.MaxQuantity = dto.MaxQuantity;
        recurringProduct.ProductInstructions = dto.ProductInstructions?.Trim();
        recurringProduct.IsActive = dto.IsActive;
        recurringProduct.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var result = new CustomerRecurringProductDto
        {
            Id = recurringProduct.Id,
            CustomerId = recurringProduct.CustomerId,
            ProductId = recurringProduct.ProductId,
            ProductName = recurringProduct.Product.Name,
            ProductSku = recurringProduct.Product.SKU,
            DefaultQuantity = recurringProduct.DefaultQuantity,
            Unit = recurringProduct.Unit,
            PriceOverride = recurringProduct.PriceOverride,
            EffectivePrice = recurringProduct.PriceOverride ?? recurringProduct.Product.SellingPrice,
            MinQuantity = recurringProduct.MinQuantity,
            MaxQuantity = recurringProduct.MaxQuantity,
            ProductInstructions = recurringProduct.ProductInstructions,
            IsActive = recurringProduct.IsActive,
            CurrentStock = recurringProduct.Product.CurrentStock
        };

        return ApiResponse<CustomerRecurringProductDto>.Ok(result, "Customer recurring product updated.");
    }

    public async Task<ApiResponse<bool>> RemoveProductAsync(Guid customerId, Guid recurringProductId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var item = await _context.CustomerRecurringProducts
            .FirstOrDefaultAsync(rp => rp.Id == recurringProductId && rp.CustomerId == customerId && rp.BusinessId == businessId, cancellationToken);

        if (item == null)
        {
            throw new NotFoundAppException("CustomerRecurringProduct", recurringProductId);
        }

        _context.CustomerRecurringProducts.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Product removed from recurring list.");
    }
}
