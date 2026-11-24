using SmartStock.Core.Common;
using SmartStock.Core.Enums;

namespace SmartStock.Core.DTOs;

public class SupplierFilterParams : PaginationParams
{
    public bool? IsActive { get; set; }
}

public class SupplierInvoiceFilterParams : PaginationParams
{
    public Guid? SupplierId { get; set; }
    public SupplierInvoiceStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? HasPriceIncreases { get; set; }
}

public class SupplierInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid SupplierInvoiceId { get; set; }

    // Extracted fields
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

    // Matching
    public Guid? MatchedProductId { get; set; }
    public string? MatchedProductName { get; set; }
    public string? MatchedProductSku { get; set; }
    public decimal? MatchedProductCurrentStock { get; set; }
    public UnitType? MatchedProductUnit { get; set; }
    public ProductMatchStatus MatchStatus { get; set; }
    public decimal MatchConfidence { get; set; }
    public string? MatchExplanation { get; set; }

    // Conversion and stock updates
    public decimal ConversionFactor { get; set; } = 1.0m;
    public decimal? PackSize { get; set; }
    public decimal CalculatedStockQuantity { get; set; }
    public UnitType TargetUnit { get; set; } = UnitType.Piece;

    // Price comparison
    public decimal? PreviousPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PercentagePriceChange { get; set; }
    public bool ShouldUpdatePurchasePrice { get; set; } = true;
    public bool IsIgnored { get; set; }
}

public class SupplierInvoiceDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCompanyName { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; }

    public string? SupplierNameRaw { get; set; }
    public string? SupplierGst { get; set; }
    public string? BillingAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PurchaseOrderNumber { get; set; }

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GrandTotal { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public SupplierInvoiceStatus Status { get; set; }
    public bool IsDuplicate { get; set; }
    public string? DuplicateReason { get; set; }
    public string? Notes { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public Guid? ConfirmedPurchaseId { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<SupplierInvoiceItemDto> Items { get; set; } = new();
    public int ItemsCount => Items.Count;
    public int MatchedItemsCount => Items.Count(i => i.MatchedProductId != null);
    public int UnmatchedItemsCount => Items.Count(i => i.MatchedProductId == null && !i.IsIgnored);
    public int PriceHikeCount => Items.Count(i => i.PriceDifference > 0);
}

public class UpdateSupplierInvoiceItemDto
{
    public Guid? MatchedProductId { get; set; }
    public ProductMatchStatus? MatchStatus { get; set; }
    public decimal? ConversionFactor { get; set; }
    public decimal? PackSize { get; set; }
    public decimal? CalculatedStockQuantity { get; set; }
    public UnitType? TargetUnit { get; set; }
    public decimal? NewPurchasePrice { get; set; }
    public bool? ShouldUpdatePurchasePrice { get; set; }
    public bool? IsIgnored { get; set; }
}

public class ConfirmProductMatchDto
{
    public Guid InvoiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal ConversionFactor { get; set; } = 1.0m;
    public decimal? PackSize { get; set; }
    public bool ShouldUpdatePurchasePrice { get; set; } = true;
}

public class CreateProductFromInvoiceDto
{
    public Guid InvoiceItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MinimumStock { get; set; } = 5;
    public decimal ConversionFactor { get; set; } = 1.0m;
    public decimal? PackSize { get; set; }
}

public class ApproveSupplierInvoiceDto
{
    public string? Notes { get; set; }
    public bool UpdatePurchasePrices { get; set; } = true;
}

public class SupplierInvoiceDashboardMetricsDto
{
    public int TotalInvoices { get; set; }
    public int ReadyForReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int DuplicateCount { get; set; }
    public decimal TotalAmountThisMonth { get; set; }
    public int TotalPriceHikesDetected { get; set; }
}

public class InvoiceComparisonDto
{
    public SupplierInvoiceDto CurrentInvoice { get; set; } = null!;
    public SupplierInvoiceDto? PreviousInvoice { get; set; }
    public List<InvoiceItemComparisonDto> ItemComparisons { get; set; } = new();
    public decimal TotalPriceVariance { get; set; }
    public decimal PercentageVariance { get; set; }
}

public class InvoiceItemComparisonDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal PriceDifference => CurrentPrice - PreviousPrice;
    public decimal PercentageChange => PreviousPrice > 0 ? ((CurrentPrice - PreviousPrice) / PreviousPrice) * 100 : 0;
    public decimal CurrentQuantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public string Unit { get; set; } = "PCS";
}

public class AIInvoiceAssistantQueryDto
{
    public string Query { get; set; } = string.Empty;
    public Guid? SupplierId { get; set; }
    public Guid? InvoiceId { get; set; }
}

public class AIInvoiceAssistantResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<string> SuggestedActions { get; set; } = new();
    public List<SupplierInvoiceDto> RelatedInvoices { get; set; } = new();
    public List<SupplierSuppliedProductDto> RelatedProducts { get; set; } = new();
}

public class SupplierSuppliedProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal LastPurchasePrice { get; set; }
    public UnitType Unit { get; set; }
    public DateTime? LastPurchasedAt { get; set; }
}

public class SupplierPurchaseHistoryDto
{
    public Guid PurchaseId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class SupplierInvoiceHistoryDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal GrandTotal { get; set; }
    public SupplierInvoiceStatus Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class SupplierDetailExtendedDto : SupplierDto
{
    public List<SupplierInvoiceHistoryDto> RecentInvoices { get; set; } = new();
    public List<SupplierPurchaseHistoryDto> RecentPurchases { get; set; } = new();
    public List<SupplierSuppliedProductDto> SuppliedProducts { get; set; } = new();
}
