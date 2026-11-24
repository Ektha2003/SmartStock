namespace SmartStock.Core.Enums;

public enum UserRole
{
    Owner = 1,
    Staff = 2
}

public enum UnitType
{
    Piece = 1,
    Kg = 2,
    Gram = 3,
    Litre = 4,
    Box = 5,
    Packet = 6
}

public enum TransactionType
{
    Opening = 1,
    Purchase = 2,
    Sale = 3,
    Adjustment = 4
}

public enum NotificationType
{
    LowStock = 1,
    OutOfStock = 2,
    ReorderRecommendation = 3,
    System = 4
}

public enum StockRiskLevel
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Healthy = 4
}

public enum ContactMethod
{
    Email = 1,
    WhatsApp = 2,
    Sms = 3,
    Phone = 4
}

public enum RecurringFrequency
{
    Every7Days = 1,
    Every15Days = 2,
    Every30Days = 3,
    EveryMonth = 4,
    Every2Months = 5,
    CustomDays = 6
}

public enum SubscriptionStatus
{
    Active = 1,
    Paused = 2,
    Cancelled = 3
}

public enum OrderSource
{
    Manual = 1,
    Recurring = 2,
    AiAssisted = 3
}

public enum RecurringOrderStatus
{
    Draft = 1,
    PendingCustomerApproval = 2,
    ModificationRequested = 3,
    Approved = 4,
    Skipped = 5,
    Confirmed = 6,
    Processing = 7,
    Dispatched = 8,
    Cancelled = 9,
    Expired = 10
}

public enum EmailDirection
{
    Outbound = 1,
    Inbound = 2
}

public enum EmailStatus
{
    Queued = 1,
    Sent = 2,
    Failed = 3,
    Received = 4,
    Processed = 5
}

public enum AIParsedIntent
{
    ApproveOrder = 1,
    ModifyOrder = 2,
    AddProduct = 3,
    RemoveProduct = 4,
    SkipOrder = 5,
    RescheduleOrder = 6,
    ChangeRecurringPreferences = 7,
    GeneralQuery = 8,
    Unknown = 9
}

public enum SupplierInvoiceStatus
{
    Uploaded = 1,
    Extracting = 2,
    ReadyForReview = 3,
    Approved = 4,
    Rejected = 5,
    Duplicate = 6
}

public enum ProductMatchStatus
{
    ExactMatch = 1,
    HighConfidenceMatch = 2,
    PossibleMatch = 3,
    NoMatch = 4,
    MultipleMatches = 5,
    UserConfirmed = 6,
    NewProductCreated = 7
}
