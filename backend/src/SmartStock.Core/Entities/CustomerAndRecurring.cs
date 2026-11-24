using SmartStock.Core.Enums;

namespace SmartStock.Core.Entities;

public class Customer : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
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

    // Navigation properties
    public Business Business { get; set; } = null!;
    public CustomerRecurringSetting? RecurringSetting { get; set; }
    public ICollection<CustomerRecurringProduct> RecurringProducts { get; set; } = new List<CustomerRecurringProduct>();
    public ICollection<RecurringOrder> RecurringOrders { get; set; } = new List<RecurringOrder>();
    public ICollection<CustomerEmailLog> EmailLogs { get; set; } = new List<CustomerEmailLog>();
    public ICollection<AIEmailProcessingLog> AIEmailLogs { get; set; } = new List<AIEmailProcessingLog>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

public class CustomerRecurringSetting : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public RecurringFrequency FrequencyType { get; set; } = RecurringFrequency.Every7Days;
    public int IntervalDays { get; set; } = 7;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextOrderDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? PreferredDeliveryDate { get; set; }
    public string? PreferredDeliveryTime { get; set; } // e.g., "09:00 - 12:00"
    public bool AutoEmailEnabled { get; set; } = true;
    public bool RequireCustomerApproval { get; set; } = true;
    public int ReminderBeforeDeliveryDays { get; set; } = 1;
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public DateTime? LastOrderGeneratedDate { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}

public class CustomerRecurringProduct : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal DefaultQuantity { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal? PriceOverride { get; set; }
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class RecurringOrder : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderSource OrderSource { get; set; } = OrderSource.Recurring;
    public RecurringOrderStatus Status { get; set; } = RecurringOrderStatus.Draft;
    public DateTime ScheduledOrderDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpectedDeliveryDate { get; set; } = DateTime.UtcNow.AddDays(1);
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
    public bool RequiresManualReview { get; set; } = false;
    public string? ManualReviewReason { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Sale? ConfirmedSale { get; set; }
    public ICollection<RecurringOrderItem> Items { get; set; } = new List<RecurringOrderItem>();
    public ICollection<RecurringOrderApprovalToken> ApprovalTokens { get; set; } = new List<RecurringOrderApprovalToken>();
    public ICollection<CustomerEmailLog> EmailLogs { get; set; } = new List<CustomerEmailLog>();
    public ICollection<AIEmailProcessingLog> AIEmailLogs { get; set; } = new List<AIEmailProcessingLog>();
}

public class RecurringOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecurringOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public string? ProductInstructions { get; set; }
    public bool IsAddedByCustomer { get; set; } = false;
    public bool IsModifiedByCustomer { get; set; } = false;
    public decimal? OriginalQuantity { get; set; }

    // Navigation properties
    public RecurringOrder RecurringOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class RecurringOrderApprovalToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecurringOrderId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ClientIp { get; set; }

    // Navigation properties
    public RecurringOrder RecurringOrder { get; set; } = null!;
}

public class CustomerEmailLog : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? RecurringOrderId { get; set; }
    public EmailDirection Direction { get; set; } = EmailDirection.Outbound;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? ThreadId { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Sent;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public RecurringOrder? RecurringOrder { get; set; }
}

public class AIEmailProcessingLog : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? RecurringOrderId { get; set; }
    public Guid? EmailLogId { get; set; }
    public string RawEmailText { get; set; } = string.Empty;
    public string ParsedIntent { get; set; } = string.Empty;
    public string ExtractedDataJson { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public bool IsActionExecuted { get; set; } = false;
    public bool RequiresManualReview { get; set; } = false;
    public string? ManualReviewReason { get; set; }
    public string? ValidationErrors { get; set; }
    public string? ExecutionSummary { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public RecurringOrder? RecurringOrder { get; set; }
    public CustomerEmailLog? EmailLog { get; set; }
}
