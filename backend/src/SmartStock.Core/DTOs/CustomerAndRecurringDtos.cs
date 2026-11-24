using SmartStock.Core.Common;
using SmartStock.Core.Enums;

namespace SmartStock.Core.DTOs;

#region Customer DTOs

public class CustomerDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public ContactMethod PreferredContactMethod { get; set; }
    public string PreferredContactMethodName => PreferredContactMethod.ToString();
    public bool RecurringOrderEnabled { get; set; }
    public string? DefaultDeliveryInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public CustomerRecurringSettingDto? RecurringSetting { get; set; }
    public int RecurringProductsCount { get; set; }
    public int TotalOrdersCount { get; set; }
    public DateTime? NextOrderDate => RecurringSetting?.NextOrderDate;
}

public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ContactMethod PreferredContactMethod { get; set; } = ContactMethod.Email;
    public bool RecurringOrderEnabled { get; set; } = false;
    public string? DefaultDeliveryInstructions { get; set; }
}

public class UpdateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public ContactMethod PreferredContactMethod { get; set; } = ContactMethod.Email;
    public bool RecurringOrderEnabled { get; set; } = false;
    public string? DefaultDeliveryInstructions { get; set; }
}

public class CustomerFilterParams : PaginationParams
{
    public bool? IsActive { get; set; }
    public bool? RecurringOnly { get; set; }
}

#endregion

#region Recurring Settings DTOs

public class CustomerRecurringSettingDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public RecurringFrequency FrequencyType { get; set; }
    public string FrequencyTypeName => FrequencyType.ToString();
    public int IntervalDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextOrderDate { get; set; }
    public DateTime? PreferredDeliveryDate { get; set; }
    public string? PreferredDeliveryTime { get; set; }
    public bool AutoEmailEnabled { get; set; }
    public bool RequireCustomerApproval { get; set; }
    public int ReminderBeforeDeliveryDays { get; set; }
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public DateTime? LastOrderGeneratedDate { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public string SubscriptionStatusName => SubscriptionStatus.ToString();
}

public class UpdateCustomerRecurringSettingDto
{
    public RecurringFrequency FrequencyType { get; set; } = RecurringFrequency.Every7Days;
    public int IntervalDays { get; set; } = 7;
    public DateTime StartDate { get; set; }
    public DateTime NextOrderDate { get; set; }
    public DateTime? PreferredDeliveryDate { get; set; }
    public string? PreferredDeliveryTime { get; set; }
    public bool AutoEmailEnabled { get; set; } = true;
    public bool RequireCustomerApproval { get; set; } = true;
    public int ReminderBeforeDeliveryDays { get; set; } = 1;
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;
}

#endregion

#region Recurring Products DTOs

public class CustomerRecurringProductDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal DefaultQuantity { get; set; }
    public UnitType Unit { get; set; }
    public string UnitName => Unit.ToString();
    public decimal? PriceOverride { get; set; }
    public decimal EffectivePrice { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsActive { get; set; }
    public decimal CurrentStock { get; set; }
    public bool InStock => CurrentStock >= DefaultQuantity;
}

public class CreateCustomerRecurringProductDto
{
    public Guid ProductId { get; set; }
    public decimal DefaultQuantity { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal? PriceOverride { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomerRecurringProductDto
{
    public decimal DefaultQuantity { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal? PriceOverride { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsActive { get; set; } = true;
}

#endregion

#region Recurring Order DTOs

public class RecurringOrderDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public OrderSource OrderSource { get; set; }
    public string OrderSourceName => OrderSource.ToString();
    public RecurringOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime ScheduledOrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public string? PreferredDeliveryTime { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CustomerNotes { get; set; }
    public string? SpecialInstructions { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime? CustomerApprovedAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public DateTime? LastCustomerResponseAt { get; set; }
    public Guid? ConfirmedSaleId { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? ManualReviewReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RecurringOrderItemDto> Items { get; set; } = new();
}

public class RecurringOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public UnitType Unit { get; set; }
    public string UnitName => Unit.ToString();
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsAddedByCustomer { get; set; }
    public bool IsModifiedByCustomer { get; set; }
    public decimal? OriginalQuantity { get; set; }
    public decimal AvailableStock { get; set; }
}

public class RecurringOrderFilterParams : PaginationParams
{
    public Guid? CustomerId { get; set; }
    public RecurringOrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? RequiresManualReview { get; set; }
    public RecurringFrequency? Frequency { get; set; }
}

public class ManualOrderReviewResolutionDto
{
    public bool Approve { get; set; }
    public string? AdminNotes { get; set; }
}

#endregion

#region Public Customer Portal DTOs

public class CustomerPortalOrderDto
{
    public string Token { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime ExpectedDeliveryDate { get; set; }
    public string? PreferredDeliveryTime { get; set; }
    public string? SpecialInstructions { get; set; }
    public RecurringOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsEditable => Status == RecurringOrderStatus.Draft || Status == RecurringOrderStatus.PendingCustomerApproval || Status == RecurringOrderStatus.ModificationRequested;
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<CustomerPortalOrderItemDto> Items { get; set; } = new();
    public List<ProductCatalogItemDto> AvailableCatalog { get; set; } = new();
}

public class CustomerPortalOrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public UnitType Unit { get; set; }
    public string UnitName => Unit.ToString();
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string? ProductInstructions { get; set; }
}

public class ProductCatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public UnitType Unit { get; set; }
    public string UnitName => Unit.ToString();
    public decimal CurrentStock { get; set; }
}

public class CustomerPortalSubmitDto
{
    public string Action { get; set; } = "APPROVE"; // APPROVE, MODIFY, SKIP
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CustomerPortalItemUpdateDto>? Items { get; set; }
}

public class CustomerPortalItemUpdateDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? ProductInstructions { get; set; }
}

#endregion

#region Email & AI Logs DTOs

public class CustomerEmailLogDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? RecurringOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public EmailDirection Direction { get; set; }
    public string DirectionName => Direction.ToString();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? ThreadId { get; set; }
    public EmailStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIEmailProcessingLogDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? RecurringOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string RawEmailText { get; set; } = string.Empty;
    public string ParsedIntent { get; set; } = string.Empty;
    public string ExtractedDataJson { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public bool IsActionExecuted { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? ManualReviewReason { get; set; }
    public string? ValidationErrors { get; set; }
    public string? ExecutionSummary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AIEmailReplyRequestDto
{
    public string FromEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? ThreadId { get; set; }
}

public class AIEmailProcessingResultDto
{
    public bool Success { get; set; }
    public string Intent { get; set; } = "UNKNOWN";
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal Confidence { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool SubscriptionChange { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? ManualReviewReason { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public string CustomerMessage { get; set; } = string.Empty;
    public List<AIExtractedItemDto> Items { get; set; } = new();
}

public class AIExtractedItemDto
{
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string Action { get; set; } = "UPDATE"; // UPDATE, ADD, REMOVE
}

public class EmailSettingsDto
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SmartStock Grocery Delivery";
    public bool EnableSsl { get; set; } = true;
    public string? GmailClientId { get; set; }
    public string? GmailClientSecret { get; set; }
    public string? GmailRefreshToken { get; set; }
}

public class RecurringDashboardMetricsDto
{
    public int TotalActiveCustomers { get; set; }
    public int CustomersWithRecurringOrders { get; set; }
    public int UpcomingRecurringOrders { get; set; }
    public int PendingCustomerApprovals { get; set; }
    public int ApprovedOrders { get; set; }
    public int SkippedOrders { get; set; }
    public int OrdersRequiringManualReview { get; set; }
    public int FailedEmailNotifications { get; set; }
}

#endregion
