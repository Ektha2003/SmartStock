using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public EmailService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string bodyHtml,
        Guid? businessId = null,
        Guid? customerId = null,
        Guid? recurringOrderId = null,
        string? threadId = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedBusinessId = businessId ?? _currentUserService.BusinessId ?? Guid.Empty;
        var fromEmail = _configuration["Email:FromEmail"] 
                        ?? _configuration["Email__FromEmail"] 
                        ?? "orders@smartstock.local";
        var fromName = _configuration["Email:FromName"] 
                       ?? _configuration["Email__FromName"] 
                       ?? "SmartStock Grocery Delivery";

        var smtpHost = _configuration["Email:SmtpHost"] ?? _configuration["Email__SmtpHost"];
        var smtpPortStr = _configuration["Email:SmtpPort"] ?? _configuration["Email__SmtpPort"] ?? "587";
        var smtpUser = _configuration["Email:SmtpUser"] ?? _configuration["Email__SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPassword"] ?? _configuration["Email__SmtpPassword"];
        var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? _configuration["Email__EnableSsl"] ?? "true");

        var messageId = $"<msg-{Guid.NewGuid():N}@{smtpHost ?? "smartstock.local"}>";

        var log = new CustomerEmailLog
        {
            Id = Guid.NewGuid(),
            BusinessId = resolvedBusinessId,
            CustomerId = customerId ?? Guid.Empty,
            RecurringOrderId = recurringOrderId,
            Direction = EmailDirection.Outbound,
            Subject = subject,
            Body = bodyHtml,
            FromEmail = fromEmail,
            ToEmail = toEmail,
            MessageId = messageId,
            ThreadId = threadId ?? messageId,
            Status = EmailStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        // If customer ID is empty, attempt to resolve via toEmail
        if (log.CustomerId == Guid.Empty && resolvedBusinessId != Guid.Empty)
        {
            var matchedCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.BusinessId == resolvedBusinessId && c.Email.ToLower() == toEmail.ToLower(), cancellationToken);
            if (matchedCustomer != null)
            {
                log.CustomerId = matchedCustomer.Id;
            }
        }

        bool sentSuccessfully = false;
        string? failureReason = null;

        // If SMTP credentials are configured, send via real SmtpClient
        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !smtpUser.Contains("your_"))
        {
            try
            {
                using var client = new SmtpClient(smtpHost, int.Parse(smtpPortStr))
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(smtpUser, smtpPass)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = bodyHtml,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                mailMessage.Headers.Add("Message-ID", messageId);
                if (!string.IsNullOrWhiteSpace(threadId))
                {
                    mailMessage.Headers.Add("In-Reply-To", threadId);
                    mailMessage.Headers.Add("References", threadId);
                }

                await client.SendMailAsync(mailMessage, cancellationToken);
                sentSuccessfully = true;
                _logger.LogInformation("Email sent successfully to {ToEmail} via SMTP ({Host})", toEmail, smtpHost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP", toEmail);
                failureReason = ex.Message;
                sentSuccessfully = false;
            }
        }
        else
        {
            // Dev/Mock mode: Log the email delivery safely
            _logger.LogInformation("[DEV EMAIL DISPATCHED] To: {ToEmail} | Subject: {Subject} | OrderId: {OrderId}", toEmail, subject, recurringOrderId);
            sentSuccessfully = true;
        }

        log.Status = sentSuccessfully ? EmailStatus.Sent : EmailStatus.Failed;
        log.ErrorMessage = failureReason;
        log.SentAt = sentSuccessfully ? DateTime.UtcNow : null;

        if (log.CustomerId != Guid.Empty)
        {
            _context.CustomerEmailLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sentSuccessfully;
    }

    public async Task<ApiResponse<PagedResult<CustomerEmailLogDto>>> GetEmailLogsAsync(
        Guid? customerId,
        Guid? orderId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = _context.CustomerEmailLogs
            .Include(l => l.Customer)
            .Include(l => l.RecurringOrder)
            .Where(l => l.BusinessId == businessId);

        if (customerId.HasValue)
        {
            query = query.Where(l => l.CustomerId == customerId.Value);
        }

        if (orderId.HasValue)
        {
            query = query.Where(l => l.RecurringOrderId == orderId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(l => new CustomerEmailLogDto
            {
                Id = l.Id,
                CustomerId = l.CustomerId,
                CustomerName = l.Customer != null ? l.Customer.Name : string.Empty,
                RecurringOrderId = l.RecurringOrderId,
                OrderNumber = l.RecurringOrder != null ? l.RecurringOrder.OrderNumber : null,
                Direction = l.Direction,
                Subject = l.Subject,
                Body = l.Body,
                FromEmail = l.FromEmail,
                ToEmail = l.ToEmail,
                MessageId = l.MessageId,
                ThreadId = l.ThreadId,
                Status = l.Status,
                ErrorMessage = l.ErrorMessage,
                SentAt = l.SentAt,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<CustomerEmailLogDto>>.Ok(
            new PagedResult<CustomerEmailLogDto>(items, totalCount, pagination.PageNumber, pagination.PageSize),
            "Email logs retrieved.");
    }

    public async Task<ApiResponse<PagedResult<AIEmailProcessingLogDto>>> GetAILogsAsync(
        Guid? customerId,
        Guid? orderId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = _context.AIEmailProcessingLogs
            .Include(l => l.Customer)
            .Include(l => l.RecurringOrder)
            .Where(l => l.BusinessId == businessId);

        if (customerId.HasValue)
        {
            query = query.Where(l => l.CustomerId == customerId.Value);
        }

        if (orderId.HasValue)
        {
            query = query.Where(l => l.RecurringOrderId == orderId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(l => new AIEmailProcessingLogDto
            {
                Id = l.Id,
                CustomerId = l.CustomerId,
                CustomerName = l.Customer != null ? l.Customer.Name : string.Empty,
                RecurringOrderId = l.RecurringOrderId,
                OrderNumber = l.RecurringOrder != null ? l.RecurringOrder.OrderNumber : null,
                RawEmailText = l.RawEmailText,
                ParsedIntent = l.ParsedIntent,
                ExtractedDataJson = l.ExtractedDataJson,
                ConfidenceScore = l.ConfidenceScore,
                IsActionExecuted = l.IsActionExecuted,
                RequiresManualReview = l.RequiresManualReview,
                ManualReviewReason = l.ManualReviewReason,
                ValidationErrors = l.ValidationErrors,
                ExecutionSummary = l.ExecutionSummary,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<AIEmailProcessingLogDto>>.Ok(
            new PagedResult<AIEmailProcessingLogDto>(items, totalCount, pagination.PageNumber, pagination.PageSize),
            "AI processing logs retrieved.");
    }
}
