using SmartStock.Core.Enums;

namespace SmartStock.Core.Entities;

public class SupplierInvoice : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid? SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; }

    // Extracted Header Info
    public string? SupplierNameRaw { get; set; }
    public string? SupplierGst { get; set; }
    public string? BillingAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PurchaseOrderNumber { get; set; }

    // Amounts & Tax Breakdown
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GrandTotal { get; set; }

    // Upload & File metadata
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Workflow & State
    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Uploaded;
    public bool IsDuplicate { get; set; } = false;
    public string? DuplicateReason { get; set; }
    public string? ExtractedRawJson { get; set; }
    public string? Notes { get; set; }

    // Approval details
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public Guid? ConfirmedPurchaseId { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public Purchase? ConfirmedPurchase { get; set; }
    public ICollection<SupplierInvoiceItem> Items { get; set; } = new List<SupplierInvoiceItem>();
    public ICollection<AIInvoiceProcessingLog> ProcessingLogs { get; set; } = new List<AIInvoiceProcessingLog>();
}

public class SupplierInvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierInvoiceId { get; set; }

    // Extracted Raw Line Item Fields
    public string RawProductName { get; set; } = string.Empty;
    public string? RawDescription { get; set; }
    public string? RawSupplierSku { get; set; }
    public string? RawHsnCode { get; set; }
    public decimal InvoiceQuantity { get; set; }
    public string InvoiceUnit { get; set; } = "PCS";
    public decimal InvoiceUnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    // Product Matching & Confidence
    public Guid? MatchedProductId { get; set; }
    public ProductMatchStatus MatchStatus { get; set; } = ProductMatchStatus.NoMatch;
    public decimal MatchConfidence { get; set; } = 0.0m;
    public string? MatchExplanation { get; set; }

    // Conversion and Inventory Stock Calculations
    public decimal ConversionFactor { get; set; } = 1.0m; // e.g. 5 for 5KG pack
    public decimal? PackSize { get; set; }
    public decimal CalculatedStockQuantity { get; set; }
    public UnitType TargetUnit { get; set; } = UnitType.Piece;

    // Price Comparison & Updates
    public decimal? PreviousPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PercentagePriceChange { get; set; }
    public bool ShouldUpdatePurchasePrice { get; set; } = true;
    public bool IsIgnored { get; set; } = false;

    // Navigation properties
    public SupplierInvoice SupplierInvoice { get; set; } = null!;
    public Product? MatchedProduct { get; set; }
}

public class ProductPurchasePriceHistory : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseId { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PriceDifference => NewPrice - OldPrice;
    public decimal PercentageChange => OldPrice > 0 ? ((NewPrice - OldPrice) / OldPrice) * 100 : 0;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public SupplierInvoice? SupplierInvoice { get; set; }
}

public class AIInvoiceProcessingLog : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public string Stage { get; set; } = string.Empty; // OCR_EXTRACTION, PRODUCT_MATCHING, APPROVAL
    public decimal ConfidenceScore { get; set; }
    public string? ExtractedDataJson { get; set; }
    public string? ExecutionSummary { get; set; }
    public string? Errors { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public SupplierInvoice SupplierInvoice { get; set; } = null!;
}
