using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class RecurringOrderSchedulerService : IRecurringOrderSchedulerService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecurringOrderService _recurringOrderService;
    private readonly IAIEmailGeneratorService _emailGeneratorService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecurringOrderSchedulerService> _logger;

    public RecurringOrderSchedulerService(
        ApplicationDbContext context,
        IRecurringOrderService recurringOrderService,
        IAIEmailGeneratorService emailGeneratorService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RecurringOrderSchedulerService> logger)
    {
        _context = context;
        _recurringOrderService = recurringOrderService;
        _emailGeneratorService = emailGeneratorService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> ProcessUpcomingRecurringOrdersCycleAsync(CancellationToken cancellationToken = default)
    {
        int processedCount = 0;
        var nowUtc = DateTime.UtcNow;

        _logger.LogInformation("Starting recurring orders automation cycle at {NowUtc}", nowUtc);

        // 1. Identify customers with active subscriptions whose next order date has arrived or is approaching
        var dueSettings = await _context.CustomerRecurringSettings
            .Include(s => s.Customer)
                .ThenInclude(c => c.RecurringProducts)
                    .ThenInclude(rp => rp.Product)
            .Where(s => s.SubscriptionStatus == SubscriptionStatus.Active &&
                        s.Customer.IsActive &&
                        s.Customer.RecurringOrderEnabled &&
                        s.NextOrderDate <= nowUtc.AddDays(1))
            .ToListAsync(cancellationToken);

        var frontendBaseUrl = _configuration["Frontend:Origin"]?.Split(';').FirstOrDefault()?.Trim().TrimEnd('/') ?? "http://localhost:5173";

        foreach (var setting in dueSettings)
        {
            var customer = setting.Customer;
            try
            {
                // Check if a draft/pending order already exists for this cycle date (Idempotency)
                var existingOrder = await _context.RecurringOrders
                    .Include(ro => ro.ApprovalTokens)
                    .Include(ro => ro.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(ro => ro.CustomerId == customer.Id &&
                                               ro.BusinessId == customer.BusinessId &&
                                               ro.ScheduledOrderDate.Date == setting.NextOrderDate.Date &&
                                               ro.Status != RecurringOrderStatus.Cancelled, cancellationToken);

                RecurringOrder targetOrder;
                if (existingOrder == null)
                {
                    // Generate draft recurring order
                    var draftRes = await _recurringOrderService.GenerateDraftOrderForCustomerAsync(customer.Id, setting.NextOrderDate, cancellationToken);
                    if (!draftRes.Success || draftRes.Data == null)
                    {
                        _logger.LogWarning("Failed to generate draft order for customer {CustomerId}: {Message}", customer.Id, draftRes.Message);
                        continue;
                    }

                    targetOrder = await _context.RecurringOrders
                        .Include(ro => ro.ApprovalTokens)
                        .Include(ro => ro.Items)
                            .ThenInclude(i => i.Product)
                        .FirstAsync(ro => ro.Id == draftRes.Data.Id, cancellationToken);

                    processedCount++;
                }
                else
                {
                    targetOrder = existingOrder;
                }

                // If email already sent, check for reminder
                var token = targetOrder.ApprovalTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.Token;
                var approvalLink = $"{frontendBaseUrl}/order-approval/{token}";

                if (setting.AutoEmailEnabled && targetOrder.Status == RecurringOrderStatus.PendingCustomerApproval)
                {
                    var emailAlreadySent = await _context.CustomerEmailLogs
                        .AnyAsync(l => l.RecurringOrderId == targetOrder.Id && l.Direction == EmailDirection.Outbound && l.Status == EmailStatus.Sent, cancellationToken);

                    if (!emailAlreadySent)
                    {
                        // Generate AI personalized email & dispatch
                        var emailBody = await _emailGeneratorService.GenerateUpcomingOrderEmailAsync(targetOrder, customer, approvalLink, cancellationToken);
                        await _emailService.SendEmailAsync(
                            customer.Email,
                            $"Your Upcoming Grocery Order – Please Confirm (#{targetOrder.OrderNumber})",
                            emailBody,
                            customer.BusinessId,
                            customer.Id,
                            targetOrder.Id,
                            null,
                            cancellationToken);

                        targetOrder.ApprovalRequestedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Dispatched upcoming recurring order email to {CustomerEmail} for order #{OrderNumber}", customer.Email, targetOrder.OrderNumber);
                    }
                    else if (!targetOrder.ReminderSentAt.HasValue &&
                             (nowUtc - targetOrder.ScheduledOrderDate).TotalDays >= setting.ReminderBeforeDeliveryDays)
                    {
                        // Send Reminder
                        var reminderSubject = $"Reminder: Please Confirm Your Grocery Delivery (#{targetOrder.OrderNumber})";
                        var emailBody = await _emailGeneratorService.GenerateUpcomingOrderEmailAsync(targetOrder, customer, approvalLink, cancellationToken);
                        await _emailService.SendEmailAsync(
                            customer.Email,
                            reminderSubject,
                            emailBody,
                            customer.BusinessId,
                            customer.Id,
                            targetOrder.Id,
                            null,
                            cancellationToken);

                        targetOrder.ReminderSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Dispatched recurring order reminder email to {CustomerEmail} for order #{OrderNumber}", customer.Email, targetOrder.OrderNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring order cycle for customer {CustomerId}", customer.Id);
            }
        }

        _logger.LogInformation("Finished recurring orders automation cycle. Processed {Count} orders.", processedCount);
        return processedCount;
    }
}
