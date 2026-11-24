using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CustomerService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<PagedResult<CustomerDto>>> GetPagedAsync(CustomerFilterParams filter, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = _context.Customers
            .Include(c => c.RecurringSetting)
            .Include(c => c.RecurringProducts)
            .Include(c => c.RecurringOrders)
            .Where(c => c.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.Email.ToLower().Contains(search) ||
                c.Phone.Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == filter.IsActive.Value);
        }

        if (filter.RecurringOnly.HasValue && filter.RecurringOnly.Value)
        {
            query = query.Where(c => c.RecurringOrderEnabled);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "email" => filter.SortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "phone" => filter.SortDescending ? query.OrderByDescending(c => c.Phone) : query.OrderBy(c => c.Phone),
            _ => filter.SortDescending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt)
        };

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<CustomerDto>>.Ok(
            new PagedResult<CustomerDto>(items, totalCount, filter.PageNumber, filter.PageSize),
            "Customers retrieved successfully.");
    }

    public async Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customer = await _context.Customers
            .Include(c => c.RecurringSetting)
            .Include(c => c.RecurringProducts)
            .Include(c => c.RecurringOrders)
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", id);
        }

        return ApiResponse<CustomerDto>.Ok(MapToDto(customer), "Customer retrieved successfully.");
    }

    public async Task<ApiResponse<CustomerDto>> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var email = dto.Email.Trim().ToLower();

        var emailExists = await _context.Customers
            .AnyAsync(c => c.BusinessId == businessId && c.Email.ToLower() == email, cancellationToken);

        if (emailExists)
        {
            throw new DuplicateEntityException("Customer", "Email", dto.Email);
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = dto.Name.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            BillingAddress = dto.BillingAddress.Trim(),
            DeliveryAddress = dto.DeliveryAddress.Trim(),
            Notes = dto.Notes?.Trim(),
            PreferredContactMethod = dto.PreferredContactMethod,
            RecurringOrderEnabled = dto.RecurringOrderEnabled,
            DefaultDeliveryInstructions = dto.DefaultDeliveryInstructions?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // If recurring order enabled, initialize a default recurring setting
        var setting = new CustomerRecurringSetting
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CustomerId = customer.Id,
            FrequencyType = RecurringFrequency.Every7Days,
            IntervalDays = 7,
            StartDate = DateTime.UtcNow,
            NextOrderDate = DateTime.UtcNow.AddDays(7),
            AutoEmailEnabled = true,
            RequireCustomerApproval = true,
            ReminderBeforeDeliveryDays = 1,
            TimeZone = "Asia/Kolkata",
            SubscriptionStatus = customer.RecurringOrderEnabled ? SubscriptionStatus.Active : SubscriptionStatus.Paused,
            CreatedAt = DateTime.UtcNow
        };

        customer.RecurringSetting = setting;
        _context.Customers.Add(customer);
        _context.CustomerRecurringSettings.Add(setting);

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerDto>.Ok(MapToDto(customer), "Customer created successfully.");
    }

    public async Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customer = await _context.Customers
            .Include(c => c.RecurringSetting)
            .Include(c => c.RecurringProducts)
            .Include(c => c.RecurringOrders)
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", id);
        }

        var email = dto.Email.Trim().ToLower();
        var emailExists = await _context.Customers
            .AnyAsync(c => c.BusinessId == businessId && c.Email.ToLower() == email && c.Id != id, cancellationToken);

        if (emailExists)
        {
            throw new DuplicateEntityException("Customer", "Email", dto.Email);
        }

        customer.Name = dto.Name.Trim();
        customer.Email = email;
        customer.Phone = dto.Phone.Trim();
        customer.BillingAddress = dto.BillingAddress.Trim();
        customer.DeliveryAddress = dto.DeliveryAddress.Trim();
        customer.Notes = dto.Notes?.Trim();
        customer.PreferredContactMethod = dto.PreferredContactMethod;
        customer.DefaultDeliveryInstructions = dto.DefaultDeliveryInstructions?.Trim();
        customer.IsActive = dto.IsActive;

        if (customer.RecurringOrderEnabled != dto.RecurringOrderEnabled)
        {
            customer.RecurringOrderEnabled = dto.RecurringOrderEnabled;
            if (customer.RecurringSetting != null)
            {
                customer.RecurringSetting.SubscriptionStatus = dto.RecurringOrderEnabled
                    ? SubscriptionStatus.Active
                    : SubscriptionStatus.Paused;
                customer.RecurringSetting.UpdatedAt = DateTime.UtcNow;
            }
            else if (dto.RecurringOrderEnabled)
            {
                var setting = new CustomerRecurringSetting
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    CustomerId = customer.Id,
                    FrequencyType = RecurringFrequency.Every7Days,
                    IntervalDays = 7,
                    StartDate = DateTime.UtcNow,
                    NextOrderDate = DateTime.UtcNow.AddDays(7),
                    AutoEmailEnabled = true,
                    RequireCustomerApproval = true,
                    ReminderBeforeDeliveryDays = 1,
                    TimeZone = "Asia/Kolkata",
                    SubscriptionStatus = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                customer.RecurringSetting = setting;
                _context.CustomerRecurringSettings.Add(setting);
            }
        }

        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerDto>.Ok(MapToDto(customer), "Customer updated successfully.");
    }

    public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", id);
        }

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(customer.IsActive, $"Customer status changed to {(customer.IsActive ? "Active" : "Inactive")}.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var customer = await _context.Customers
            .Include(c => c.RecurringOrders)
            .Include(c => c.Sales)
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", id);
        }

        if (customer.RecurringOrders.Count > 0 || customer.Sales.Count > 0)
        {
            // Soft-deactivate if order history exists
            customer.IsActive = false;
            customer.RecurringOrderEnabled = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return ApiResponse<bool>.Ok(true, "Customer deactivated because associated order history exists.");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Customer deleted successfully.");
    }

    public async Task<ApiResponse<PagedResult<RecurringOrderDto>>> GetOrderHistoryAsync(Guid customerId, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = _context.RecurringOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.BusinessId == businessId && o.CustomerId == customerId)
            .OrderByDescending(o => o.ScheduledOrderDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(MapOrderToDto).ToList();

        return ApiResponse<PagedResult<RecurringOrderDto>>.Ok(
            new PagedResult<RecurringOrderDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize),
            "Order history retrieved successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<RecurringOrderDto>>> GetUpcomingOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var orders = await _context.RecurringOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.BusinessId == businessId &&
                        o.CustomerId == customerId &&
                        o.Status != RecurringOrderStatus.Cancelled &&
                        o.Status != RecurringOrderStatus.Dispatched &&
                        o.Status != RecurringOrderStatus.Skipped &&
                        o.ScheduledOrderDate >= DateTime.UtcNow.AddDays(-1))
            .OrderBy(o => o.ScheduledOrderDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(MapOrderToDto).ToList();
        return ApiResponse<IReadOnlyList<RecurringOrderDto>>.Ok(dtos, "Upcoming recurring orders retrieved.");
    }

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto
        {
            Id = c.Id,
            BusinessId = c.BusinessId,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            BillingAddress = c.BillingAddress,
            DeliveryAddress = c.DeliveryAddress,
            IsActive = c.IsActive,
            Notes = c.Notes,
            PreferredContactMethod = c.PreferredContactMethod,
            RecurringOrderEnabled = c.RecurringOrderEnabled,
            DefaultDeliveryInstructions = c.DefaultDeliveryInstructions,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            RecurringProductsCount = c.RecurringProducts?.Count ?? 0,
            TotalOrdersCount = c.RecurringOrders?.Count ?? 0,
            RecurringSetting = c.RecurringSetting != null ? new CustomerRecurringSettingDto
            {
                Id = c.RecurringSetting.Id,
                CustomerId = c.RecurringSetting.CustomerId,
                FrequencyType = c.RecurringSetting.FrequencyType,
                IntervalDays = c.RecurringSetting.IntervalDays,
                StartDate = c.RecurringSetting.StartDate,
                NextOrderDate = c.RecurringSetting.NextOrderDate,
                PreferredDeliveryDate = c.RecurringSetting.PreferredDeliveryDate,
                PreferredDeliveryTime = c.RecurringSetting.PreferredDeliveryTime,
                AutoEmailEnabled = c.RecurringSetting.AutoEmailEnabled,
                RequireCustomerApproval = c.RecurringSetting.RequireCustomerApproval,
                ReminderBeforeDeliveryDays = c.RecurringSetting.ReminderBeforeDeliveryDays,
                TimeZone = c.RecurringSetting.TimeZone,
                LastOrderGeneratedDate = c.RecurringSetting.LastOrderGeneratedDate,
                SubscriptionStatus = c.RecurringSetting.SubscriptionStatus
            } : null
        };
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
