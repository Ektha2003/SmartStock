using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;

namespace SmartStock.Core.Interfaces;

public interface ICustomerService
{
    Task<ApiResponse<PagedResult<CustomerDto>>> GetPagedAsync(CustomerFilterParams filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerDto>> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<RecurringOrderDto>>> GetOrderHistoryAsync(Guid customerId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<RecurringOrderDto>>> GetUpcomingOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public interface ICustomerRecurringProductService
{
    Task<ApiResponse<IReadOnlyList<CustomerRecurringProductDto>>> GetProductsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerRecurringProductDto>> AddProductAsync(Guid customerId, CreateCustomerRecurringProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerRecurringProductDto>> UpdateProductAsync(Guid customerId, Guid recurringProductId, UpdateCustomerRecurringProductDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RemoveProductAsync(Guid customerId, Guid recurringProductId, CancellationToken cancellationToken = default);
}

public interface ICustomerRecurringSettingService
{
    Task<ApiResponse<CustomerRecurringSettingDto>> GetSettingAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerRecurringSettingDto>> UpdateSettingAsync(Guid customerId, UpdateCustomerRecurringSettingDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerRecurringSettingDto>> ToggleSubscriptionStatusAsync(Guid customerId, SubscriptionStatus status, CancellationToken cancellationToken = default);
}

public interface IRecurringOrderService
{
    Task<ApiResponse<PagedResult<RecurringOrderDto>>> GetPagedAsync(RecurringOrderFilterParams filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecurringOrderDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecurringOrderDto>> GenerateDraftOrderForCustomerAsync(Guid customerId, DateTime scheduledDate, CancellationToken cancellationToken = default);
    Task<ApiResponse<SaleDto>> FinalizeOrderToSaleAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CancelOrderAsync(Guid orderId, string? reason = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecurringOrderDto>> ResolveManualReviewAsync(Guid orderId, ManualOrderReviewResolutionDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecurringDashboardMetricsDto>> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerPortalService
{
    Task<ApiResponse<CustomerPortalOrderDto>> GetOrderByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerPortalOrderDto>> SubmitOrderDecisionAsync(string token, CustomerPortalSubmitDto dto, string? clientIp = null, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string bodyHtml, Guid? businessId = null, Guid? customerId = null, Guid? recurringOrderId = null, string? threadId = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<CustomerEmailLogDto>>> GetEmailLogsAsync(Guid? customerId, Guid? orderId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<AIEmailProcessingLogDto>>> GetAILogsAsync(Guid? customerId, Guid? orderId, PaginationParams pagination, CancellationToken cancellationToken = default);
}

public interface IAIEmailGeneratorService
{
    Task<string> GenerateUpcomingOrderEmailAsync(RecurringOrder order, Customer customer, string approvalPortalUrl, CancellationToken cancellationToken = default);
    Task<string> GenerateOrderConfirmationEmailAsync(RecurringOrder order, Customer customer, CancellationToken cancellationToken = default);
    Task<string> GenerateOrderSkippedEmailAsync(RecurringOrder order, Customer customer, CancellationToken cancellationToken = default);
}

public interface IAIEmailReplyProcessor
{
    Task<ApiResponse<AIEmailProcessingResultDto>> ProcessReplyAsync(AIEmailReplyRequestDto reply, CancellationToken cancellationToken = default);
}

public interface IRecurringOrderSchedulerService
{
    Task<int> ProcessUpcomingRecurringOrdersCycleAsync(CancellationToken cancellationToken = default);
}
