using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class CustomerPortalService : ICustomerPortalService
{
    private readonly ApplicationDbContext _context;

    public CustomerPortalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CustomerPortalOrderDto>> GetOrderByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cleanToken = token.Trim().ToLowerInvariant();
        var tokenRecord = await _context.RecurringOrderApprovalTokens
            .Include(t => t.RecurringOrder)
                .ThenInclude(ro => ro.Customer)
            .Include(t => t.RecurringOrder)
                .ThenInclude(ro => ro.Items)
                    .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Token.ToLower() == cleanToken, cancellationToken);

        if (tokenRecord == null)
        {
            throw new NotFoundAppException("Approval Link", "Invalid or expired link. Please request a new confirmation link.");
        }

        if (tokenRecord.ExpiresAt < DateTime.UtcNow)
        {
            return ApiResponse<CustomerPortalOrderDto>.Fail("This order confirmation link has expired. Please contact the store or wait for the next scheduled reminder.");
        }

        var order = tokenRecord.RecurringOrder;

        // Fetch catalog items for adding new products
        var availableProducts = await _context.Products
            .Where(p => p.BusinessId == order.BusinessId && p.IsActive && p.CurrentStock > 0)
            .OrderBy(p => p.Name)
            .Select(p => new ProductCatalogItemDto
            {
                Id = p.Id,
                Name = p.Name,
                SellingPrice = p.SellingPrice,
                Unit = p.Unit,
                CurrentStock = p.CurrentStock
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        var dto = new CustomerPortalOrderDto
        {
            Token = cleanToken,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.Name ?? "Valued Customer",
            DeliveryAddress = order.Customer?.DeliveryAddress ?? string.Empty,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            PreferredDeliveryTime = order.PreferredDeliveryTime,
            SpecialInstructions = order.SpecialInstructions,
            Status = order.Status,
            SubTotal = order.SubTotal,
            TotalAmount = order.TotalAmount,
            ExpiresAt = tokenRecord.ExpiresAt,
            Items = order.Items.Select(i => new CustomerPortalOrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                ProductInstructions = i.ProductInstructions
            }).ToList(),
            AvailableCatalog = availableProducts
        };

        return ApiResponse<CustomerPortalOrderDto>.Ok(dto, "Order details loaded.");
    }

    public async Task<ApiResponse<CustomerPortalOrderDto>> SubmitOrderDecisionAsync(string token, CustomerPortalSubmitDto dto, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var cleanToken = token.Trim().ToLowerInvariant();
        var tokenRecord = await _context.RecurringOrderApprovalTokens
            .Include(t => t.RecurringOrder)
                .ThenInclude(ro => ro.Customer)
            .Include(t => t.RecurringOrder)
                .ThenInclude(ro => ro.Items)
                    .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Token.ToLower() == cleanToken, cancellationToken);

        if (tokenRecord == null)
        {
            throw new NotFoundAppException("Approval Link", "Invalid or expired link.");
        }

        if (tokenRecord.ExpiresAt < DateTime.UtcNow)
        {
            return ApiResponse<CustomerPortalOrderDto>.Fail("This confirmation link has expired.");
        }

        var order = tokenRecord.RecurringOrder;
        if (order.Status == RecurringOrderStatus.Confirmed || order.Status == RecurringOrderStatus.Dispatched)
        {
            return ApiResponse<CustomerPortalOrderDto>.Fail("This order has already been finalized and dispatched. Please contact the store directly for changes.");
        }

        var action = dto.Action.Trim().ToUpperInvariant();

        switch (action)
        {
            case "APPROVE":
                order.Status = RecurringOrderStatus.Approved;
                order.CustomerApprovedAt = DateTime.UtcNow;
                order.LastCustomerResponseAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.SpecialInstructions))
                {
                    order.SpecialInstructions = dto.SpecialInstructions.Trim();
                }
                tokenRecord.IsUsed = true;
                tokenRecord.UsedAt = DateTime.UtcNow;
                tokenRecord.ClientIp = clientIp;
                break;

            case "SKIP":
                order.Status = RecurringOrderStatus.Skipped;
                order.LastCustomerResponseAt = DateTime.UtcNow;
                order.CustomerNotes = "Skipped by customer via approval portal.";
                tokenRecord.IsUsed = true;
                tokenRecord.UsedAt = DateTime.UtcNow;
                tokenRecord.ClientIp = clientIp;
                break;

            case "MODIFY":
                if (dto.Items == null || dto.Items.Count == 0)
                {
                    return ApiResponse<CustomerPortalOrderDto>.Fail("At least one product is required. If you want to cancel this delivery, choose 'Skip Delivery'.");
                }

                // Verify and reload catalog products
                var submittedProductIds = dto.Items.Select(x => x.ProductId).Distinct().ToList();
                var catalogProducts = await _context.Products
                    .Where(p => p.BusinessId == order.BusinessId && submittedProductIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken);

                var existingMap = order.Items.ToDictionary(i => i.ProductId);
                var updatedItems = new List<RecurringOrderItem>();
                decimal newTotal = 0;

                foreach (var itemUpdate in dto.Items)
                {
                    if (!catalogProducts.TryGetValue(itemUpdate.ProductId, out var product))
                    {
                        return ApiResponse<CustomerPortalOrderDto>.Fail($"Product ID {itemUpdate.ProductId} is not available in our store catalog.");
                    }

                    if (!product.IsActive)
                    {
                        return ApiResponse<CustomerPortalOrderDto>.Fail($"Product '{product.Name}' is currently unavailable.");
                    }

                    if (itemUpdate.Quantity <= 0)
                    {
                        continue; // skip items with 0 qty
                    }

                    var unitPrice = product.SellingPrice;
                    var subTotal = Math.Round(itemUpdate.Quantity * unitPrice, 2);
                    newTotal += subTotal;

                    if (existingMap.TryGetValue(itemUpdate.ProductId, out var existing))
                    {
                        existing.Quantity = itemUpdate.Quantity;
                        existing.UnitPrice = unitPrice;
                        existing.SubTotal = subTotal;
                        existing.ProductInstructions = itemUpdate.ProductInstructions?.Trim();
                        existing.IsModifiedByCustomer = true;
                        updatedItems.Add(existing);
                    }
                    else
                    {
                        var newItem = new RecurringOrderItem
                        {
                            Id = Guid.NewGuid(),
                            RecurringOrderId = order.Id,
                            ProductId = product.Id,
                            Quantity = itemUpdate.Quantity,
                            Unit = product.Unit,
                            UnitPrice = unitPrice,
                            SubTotal = subTotal,
                            ProductInstructions = itemUpdate.ProductInstructions?.Trim(),
                            IsAddedByCustomer = true,
                            IsModifiedByCustomer = true
                        };
                        _context.RecurringOrderItems.Add(newItem);
                        updatedItems.Add(newItem);
                    }
                }

                // Remove items that customer removed
                foreach (var (pId, existingItem) in existingMap)
                {
                    if (!updatedItems.Any(i => i.ProductId == pId))
                    {
                        _context.RecurringOrderItems.Remove(existingItem);
                    }
                }

                if (order.Items.Count == 0)
                {
                    return ApiResponse<CustomerPortalOrderDto>.Fail("No valid items remain in your order.");
                }

                order.SubTotal = newTotal;
                order.TotalAmount = newTotal;

                if (dto.RequestedDeliveryDate.HasValue)
                {
                    order.ExpectedDeliveryDate = DateTime.SpecifyKind(dto.RequestedDeliveryDate.Value, DateTimeKind.Utc);
                }

                if (!string.IsNullOrWhiteSpace(dto.SpecialInstructions))
                {
                    order.SpecialInstructions = dto.SpecialInstructions.Trim();
                }

                order.Status = RecurringOrderStatus.Approved;
                order.CustomerApprovedAt = DateTime.UtcNow;
                order.LastCustomerResponseAt = DateTime.UtcNow;
                tokenRecord.IsUsed = true;
                tokenRecord.UsedAt = DateTime.UtcNow;
                tokenRecord.ClientIp = clientIp;
                break;

            default:
                return ApiResponse<CustomerPortalOrderDto>.Fail($"Unsupported action '{dto.Action}'. Valid actions: APPROVE, MODIFY, SKIP.");
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetOrderByTokenAsync(token, cancellationToken);
    }
}
