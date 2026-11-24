using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class RecurringOrderService : IRecurringOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISalesService _salesService;

    public RecurringOrderService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISalesService salesService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _salesService = salesService;
    }

    public async Task<ApiResponse<PagedResult<RecurringOrderDto>>> GetPagedAsync(RecurringOrderFilterParams filter, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = _context.RecurringOrders
            .Include(ro => ro.Customer)
                .ThenInclude(c => c.RecurringSetting)
            .Include(ro => ro.Items)
                .ThenInclude(i => i.Product)
            .Where(ro => ro.BusinessId == businessId);

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(ro => ro.CustomerId == filter.CustomerId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(ro => ro.Status == filter.Status.Value);
        }

        if (filter.RequiresManualReview.HasValue)
        {
            query = query.Where(ro => ro.RequiresManualReview == filter.RequiresManualReview.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(ro => ro.ScheduledOrderDate >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(ro => ro.ScheduledOrderDate <= toUtc);
        }

        if (filter.Frequency.HasValue)
        {
            query = query.Where(ro => ro.Customer.RecurringSetting != null && ro.Customer.RecurringSetting.FrequencyType == filter.Frequency.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(ro =>
                ro.OrderNumber.ToLower().Contains(search) ||
                ro.Customer.Name.ToLower().Contains(search) ||
                ro.Customer.Email.ToLower().Contains(search) ||
                ro.Customer.Phone.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "ordernumber" => filter.SortDescending ? query.OrderByDescending(ro => ro.OrderNumber) : query.OrderBy(ro => ro.OrderNumber),
            "customername" => filter.SortDescending ? query.OrderByDescending(ro => ro.Customer.Name) : query.OrderBy(ro => ro.Customer.Name),
            "status" => filter.SortDescending ? query.OrderByDescending(ro => ro.Status) : query.OrderBy(ro => ro.Status),
            "totalamount" => filter.SortDescending ? query.OrderByDescending(ro => ro.TotalAmount) : query.OrderBy(ro => ro.TotalAmount),
            _ => filter.SortDescending ? query.OrderBy(ro => ro.ScheduledOrderDate) : query.OrderByDescending(ro => ro.ScheduledOrderDate)
        };

        var orders = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(MapOrderToDto).ToList();

        return ApiResponse<PagedResult<RecurringOrderDto>>.Ok(
            new PagedResult<RecurringOrderDto>(dtos, totalCount, filter.PageNumber, filter.PageSize),
            "Recurring orders retrieved.");
    }

    public async Task<ApiResponse<RecurringOrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var order = await _context.RecurringOrders
            .Include(ro => ro.Customer)
            .Include(ro => ro.Items)
                .ThenInclude(i => i.Product)
            .Include(ro => ro.ApprovalTokens)
            .FirstOrDefaultAsync(ro => ro.Id == id && ro.BusinessId == businessId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundAppException("RecurringOrder", id);
        }

        return ApiResponse<RecurringOrderDto>.Ok(MapOrderToDto(order), "Recurring order retrieved.");
    }

    public async Task<ApiResponse<RecurringOrderDto>> GenerateDraftOrderForCustomerAsync(Guid customerId, DateTime scheduledDate, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var customer = await _context.Customers
            .Include(c => c.RecurringSetting)
            .Include(c => c.RecurringProducts)
                .ThenInclude(rp => rp.Product)
            .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", customerId);
        }

        if (!customer.IsActive || !customer.RecurringOrderEnabled)
        {
            throw new ValidationAppException("CustomerId", $"Customer '{customer.Name}' is inactive or has recurring orders disabled.");
        }

        var activeProducts = customer.RecurringProducts.Where(rp => rp.IsActive && rp.Product.IsActive).ToList();
        if (activeProducts.Count == 0)
        {
            throw new ValidationAppException("RecurringProducts", $"Customer '{customer.Name}' has no active recurring products configured.");
        }

        var scheduledDateUtc = DateTime.SpecifyKind(scheduledDate.Date, DateTimeKind.Utc);

        // Idempotency check: Check if an order already exists for this customer within the same cycle day
        var existingOrder = await _context.RecurringOrders
            .FirstOrDefaultAsync(ro => ro.CustomerId == customerId &&
                                       ro.BusinessId == businessId &&
                                       ro.ScheduledOrderDate.Date == scheduledDateUtc.Date &&
                                       ro.Status != RecurringOrderStatus.Cancelled, cancellationToken);

        if (existingOrder != null)
        {
            return ApiResponse<RecurringOrderDto>.Ok(MapOrderToDto(existingOrder), "Order already generated for this cycle.");
        }

        // Generate Unique Order Number: REC-YYYYMMDD-XXXX
        var datePrefix = $"REC-{DateTime.UtcNow:yyyyMMdd}";
        var countToday = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId && ro.OrderNumber.StartsWith(datePrefix), cancellationToken);
        var orderNumber = $"{datePrefix}-{(countToday + 1):D4}";

        var expectedDeliveryDate = scheduledDateUtc.AddDays(1);
        if (customer.RecurringSetting?.PreferredDeliveryDate.HasValue == true)
        {
            expectedDeliveryDate = customer.RecurringSetting.PreferredDeliveryDate.Value;
        }

        var order = new RecurringOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customerId,
            OrderNumber = orderNumber,
            OrderSource = OrderSource.Recurring,
            Status = RecurringOrderStatus.PendingCustomerApproval,
            ScheduledOrderDate = scheduledDateUtc,
            ExpectedDeliveryDate = expectedDeliveryDate,
            PreferredDeliveryTime = customer.RecurringSetting?.PreferredDeliveryTime ?? "Morning 09:00 - 12:00",
            SpecialInstructions = customer.DefaultDeliveryInstructions,
            ApprovalRequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        foreach (var rp in activeProducts)
        {
            var unitPrice = rp.PriceOverride ?? rp.Product.SellingPrice;
            var subTotal = Math.Round(rp.DefaultQuantity * unitPrice, 2);
            totalAmount += subTotal;

            order.Items.Add(new RecurringOrderItem
            {
                Id = Guid.NewGuid(),
                RecurringOrderId = order.Id,
                ProductId = rp.ProductId,
                Quantity = rp.DefaultQuantity,
                OriginalQuantity = rp.DefaultQuantity,
                Unit = rp.Unit,
                UnitPrice = unitPrice,
                SubTotal = subTotal,
                ProductInstructions = rp.ProductInstructions
            });
        }

        order.SubTotal = totalAmount;
        order.TotalAmount = totalAmount;

        // Generate Approval Token (72 hours expiration)
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenString = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var token = new RecurringOrderApprovalToken
        {
            Id = Guid.NewGuid(),
            RecurringOrderId = order.Id,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow
        };

        order.ApprovalTokens.Add(token);
        _context.RecurringOrders.Add(order);

        // Update customer setting last order generated date
        if (customer.RecurringSetting != null)
        {
            customer.RecurringSetting.LastOrderGeneratedDate = DateTime.UtcNow;
            customer.RecurringSetting.NextOrderDate = scheduledDateUtc.AddDays(customer.RecurringSetting.IntervalDays);
            customer.RecurringSetting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Re-load with products
        var createdOrder = await _context.RecurringOrders
            .Include(ro => ro.Customer)
            .Include(ro => ro.Items).ThenInclude(i => i.Product)
            .FirstAsync(ro => ro.Id == order.Id, cancellationToken);

        return ApiResponse<RecurringOrderDto>.Ok(MapOrderToDto(createdOrder), "Draft recurring order generated successfully.");
    }

    public async Task<ApiResponse<SaleDto>> FinalizeOrderToSaleAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var order = await _context.RecurringOrders
            .Include(ro => ro.Customer)
            .Include(ro => ro.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(ro => ro.Id == orderId && ro.BusinessId == businessId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundAppException("RecurringOrder", orderId);
        }

        if (order.Status == RecurringOrderStatus.Confirmed && order.ConfirmedSaleId.HasValue)
        {
            var existingSale = await _salesService.GetSaleByIdAsync(order.ConfirmedSaleId.Value, cancellationToken);
            return ApiResponse<SaleDto>.Ok(existingSale.Data!, "Order already confirmed.");
        }

        // Create Sale using existing SalesService logic to execute transactions, deduct inventory, and handle alerts
        var saleDto = new CreateSaleDto
        {
            SaleDate = DateTime.UtcNow,
            Notes = $"Recurring Order {order.OrderNumber} for Customer: {order.Customer?.Name}",
            Items = order.Items.Select(i => new CreateSaleItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        var saleResponse = await _salesService.CreateSaleAsync(saleDto, cancellationToken);
        if (!saleResponse.Success || saleResponse.Data == null)
        {
            throw new AppException(saleResponse.Message ?? "Failed to finalize recurring order into sale.");
        }

        // Link Customer and OrderSource to the created Sale in DB
        var sale = await _context.Sales.FindAsync([saleResponse.Data.Id], cancellationToken);
        if (sale != null)
        {
            sale.CustomerId = order.CustomerId;
            sale.OrderSource = OrderSource.Recurring;
        }

        order.Status = RecurringOrderStatus.Confirmed;
        order.ConfirmedSaleId = saleResponse.Data.Id;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SaleDto>.Ok(saleResponse.Data, "Recurring order confirmed and finalized into active sales invoice.");
    }

    public async Task<ApiResponse<bool>> CancelOrderAsync(Guid orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var order = await _context.RecurringOrders
            .FirstOrDefaultAsync(ro => ro.Id == orderId && ro.BusinessId == businessId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundAppException("RecurringOrder", orderId);
        }

        if (order.Status == RecurringOrderStatus.Confirmed)
        {
            throw new ValidationAppException("Status", "Cannot cancel an order that has already been confirmed and finalized into a sale.");
        }

        order.Status = RecurringOrderStatus.Cancelled;
        order.ManualReviewReason = reason?.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Recurring order cancelled.");
    }

    public async Task<ApiResponse<RecurringOrderDto>> ResolveManualReviewAsync(Guid orderId, ManualOrderReviewResolutionDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var order = await _context.RecurringOrders
            .Include(ro => ro.Customer)
            .Include(ro => ro.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(ro => ro.Id == orderId && ro.BusinessId == businessId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundAppException("RecurringOrder", orderId);
        }

        order.RequiresManualReview = false;
        order.ManualReviewReason = string.IsNullOrWhiteSpace(dto.AdminNotes)
            ? $"Resolved by admin (Approved: {dto.Approve})"
            : $"Resolved by admin: {dto.AdminNotes.Trim()}";

        if (dto.Approve)
        {
            order.Status = RecurringOrderStatus.Approved;
            order.CustomerApprovedAt = DateTime.UtcNow;
        }
        else
        {
            order.Status = RecurringOrderStatus.Cancelled;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<RecurringOrderDto>.Ok(MapOrderToDto(order), "Manual review resolved.");
    }

    public async Task<ApiResponse<RecurringDashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var totalActiveCustomers = await _context.Customers
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var customersWithRecurring = await _context.Customers
            .CountAsync(c => c.BusinessId == businessId && c.IsActive && c.RecurringOrderEnabled, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var upcomingOrders = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId &&
                              ro.ScheduledOrderDate >= today &&
                              ro.Status != RecurringOrderStatus.Cancelled &&
                              ro.Status != RecurringOrderStatus.Confirmed &&
                              ro.Status != RecurringOrderStatus.Skipped, cancellationToken);

        var pendingApprovals = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId &&
                              ro.Status == RecurringOrderStatus.PendingCustomerApproval, cancellationToken);

        var approvedOrders = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId &&
                              ro.Status == RecurringOrderStatus.Approved, cancellationToken);

        var skippedOrders = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId &&
                              ro.Status == RecurringOrderStatus.Skipped, cancellationToken);

        var requiringManualReview = await _context.RecurringOrders
            .CountAsync(ro => ro.BusinessId == businessId &&
                              ro.RequiresManualReview, cancellationToken);

        var failedEmails = await _context.CustomerEmailLogs
            .CountAsync(el => el.BusinessId == businessId &&
                              el.Status == EmailStatus.Failed, cancellationToken);

        var metrics = new RecurringDashboardMetricsDto
        {
            TotalActiveCustomers = totalActiveCustomers,
            CustomersWithRecurringOrders = customersWithRecurring,
            UpcomingRecurringOrders = upcomingOrders,
            PendingCustomerApprovals = pendingApprovals,
            ApprovedOrders = approvedOrders,
            SkippedOrders = skippedOrders,
            OrdersRequiringManualReview = requiringManualReview,
            FailedEmailNotifications = failedEmails
        };

        return ApiResponse<RecurringDashboardMetricsDto>.Ok(metrics, "Dashboard metrics retrieved.");
    }

    private static RecurringOrderDto MapOrderToDto(RecurringOrder o)
    {
        return new RecurringOrderDto
        {
            Id = o.Id,
            BusinessId = o.BusinessId,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.Name ?? string.Empty,
            CustomerEmail = o.Customer?.Email ?? string.Empty,
            CustomerPhone = o.Customer?.Phone ?? string.Empty,
            DeliveryAddress = o.Customer?.DeliveryAddress ?? string.Empty,
            OrderNumber = o.OrderNumber,
            OrderSource = o.OrderSource,
            Status = o.Status,
            ScheduledOrderDate = o.ScheduledOrderDate,
            ExpectedDeliveryDate = o.ExpectedDeliveryDate,
            PreferredDeliveryTime = o.PreferredDeliveryTime,
            SubTotal = o.SubTotal,
            TotalAmount = o.TotalAmount,
            CustomerNotes = o.CustomerNotes,
            SpecialInstructions = o.SpecialInstructions,
            ApprovalRequestedAt = o.ApprovalRequestedAt,
            CustomerApprovedAt = o.CustomerApprovedAt,
            ReminderSentAt = o.ReminderSentAt,
            ConfirmedSaleId = o.ConfirmedSaleId,
            RequiresManualReview = o.RequiresManualReview,
            ManualReviewReason = o.ManualReviewReason,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new RecurringOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductSku = i.Product?.SKU ?? string.Empty,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                ProductInstructions = i.ProductInstructions,
                IsAddedByCustomer = i.IsAddedByCustomer,
                IsModifiedByCustomer = i.IsModifiedByCustomer,
                OriginalQuantity = i.OriginalQuantity,
                AvailableStock = i.Product?.CurrentStock ?? 0
            }).ToList()
        };
    }
}
