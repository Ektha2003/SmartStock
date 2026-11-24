using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class CustomerRecurringSettingService : ICustomerRecurringSettingService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CustomerRecurringSettingService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<CustomerRecurringSettingDto>> GetSettingAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var setting = await _context.CustomerRecurringSettings
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.BusinessId == businessId, cancellationToken);

        if (setting == null)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

            if (customer == null)
            {
                throw new NotFoundAppException("Customer", customerId);
            }

            // Create default setting if missing
            setting = new CustomerRecurringSetting
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerId = customerId,
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

            _context.CustomerRecurringSettings.Add(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<CustomerRecurringSettingDto>.Ok(MapToDto(setting), "Recurring settings retrieved.");
    }

    public async Task<ApiResponse<CustomerRecurringSettingDto>> UpdateSettingAsync(Guid customerId, UpdateCustomerRecurringSettingDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var setting = await _context.CustomerRecurringSettings
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.BusinessId == businessId, cancellationToken);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", customerId);
        }

        if (setting == null)
        {
            setting = new CustomerRecurringSetting
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomerRecurringSettings.Add(setting);
        }

        int calculatedInterval = dto.FrequencyType switch
        {
            RecurringFrequency.Every7Days => 7,
            RecurringFrequency.Every15Days => 15,
            RecurringFrequency.Every30Days => 30,
            RecurringFrequency.EveryMonth => 30,
            RecurringFrequency.Every2Months => 60,
            RecurringFrequency.CustomDays => dto.IntervalDays > 0 ? dto.IntervalDays : 7,
            _ => 7
        };

        setting.FrequencyType = dto.FrequencyType;
        setting.IntervalDays = calculatedInterval;
        setting.StartDate = dto.StartDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        setting.NextOrderDate = dto.NextOrderDate == default ? setting.StartDate.AddDays(calculatedInterval) : DateTime.SpecifyKind(dto.NextOrderDate, DateTimeKind.Utc);
        setting.PreferredDeliveryDate = dto.PreferredDeliveryDate.HasValue ? DateTime.SpecifyKind(dto.PreferredDeliveryDate.Value, DateTimeKind.Utc) : null;
        setting.PreferredDeliveryTime = dto.PreferredDeliveryTime?.Trim();
        setting.AutoEmailEnabled = dto.AutoEmailEnabled;
        setting.RequireCustomerApproval = dto.RequireCustomerApproval;
        setting.ReminderBeforeDeliveryDays = dto.ReminderBeforeDeliveryDays;
        setting.TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "Asia/Kolkata" : dto.TimeZone.Trim();
        setting.SubscriptionStatus = dto.SubscriptionStatus;
        setting.UpdatedAt = DateTime.UtcNow;

        customer.RecurringOrderEnabled = dto.SubscriptionStatus == SubscriptionStatus.Active;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerRecurringSettingDto>.Ok(MapToDto(setting), "Recurring settings updated successfully.");
    }

    public async Task<ApiResponse<CustomerRecurringSettingDto>> ToggleSubscriptionStatusAsync(Guid customerId, SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var setting = await _context.CustomerRecurringSettings
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.BusinessId == businessId, cancellationToken);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundAppException("Customer", customerId);
        }

        if (setting == null)
        {
            setting = new CustomerRecurringSetting
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerId = customerId,
                FrequencyType = RecurringFrequency.Every7Days,
                IntervalDays = 7,
                StartDate = DateTime.UtcNow,
                NextOrderDate = DateTime.UtcNow.AddDays(7),
                AutoEmailEnabled = true,
                RequireCustomerApproval = true,
                ReminderBeforeDeliveryDays = 1,
                TimeZone = "Asia/Kolkata",
                SubscriptionStatus = status,
                CreatedAt = DateTime.UtcNow
            };
            _context.CustomerRecurringSettings.Add(setting);
        }
        else
        {
            setting.SubscriptionStatus = status;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        customer.RecurringOrderEnabled = status == SubscriptionStatus.Active;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CustomerRecurringSettingDto>.Ok(MapToDto(setting), $"Subscription status updated to {status}.");
    }

    private static CustomerRecurringSettingDto MapToDto(CustomerRecurringSetting s)
    {
        return new CustomerRecurringSettingDto
        {
            Id = s.Id,
            CustomerId = s.CustomerId,
            FrequencyType = s.FrequencyType,
            IntervalDays = s.IntervalDays,
            StartDate = s.StartDate,
            NextOrderDate = s.NextOrderDate,
            PreferredDeliveryDate = s.PreferredDeliveryDate,
            PreferredDeliveryTime = s.PreferredDeliveryTime,
            AutoEmailEnabled = s.AutoEmailEnabled,
            RequireCustomerApproval = s.RequireCustomerApproval,
            ReminderBeforeDeliveryDays = s.ReminderBeforeDeliveryDays,
            TimeZone = s.TimeZone,
            LastOrderGeneratedDate = s.LastOrderGeneratedDate,
            SubscriptionStatus = s.SubscriptionStatus
        };
    }
}
