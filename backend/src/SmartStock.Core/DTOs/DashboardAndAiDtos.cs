using SmartStock.Core.Enums;

namespace SmartStock.Core.DTOs;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalSalesToday { get; set; }
    public decimal TotalSalesThisMonth { get; set; }
    public int LowStockProductCount { get; set; }
    public int OutOfStockProductCount { get; set; }
    public List<SaleDto> RecentSales { get; set; } = new();
    public List<PurchaseDto> RecentPurchases { get; set; } = new();
}

public class SalesChartPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
}

public class SalesChartResponseDto
{
    public string Interval { get; set; } = "daily"; // daily or monthly
    public List<SalesChartPointDto> DataPoints { get; set; } = new();
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CurrentStock { get; set; }
}

public class StockValueCategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal StockValue { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public string NotificationTypeName => NotificationType.ToString();
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForecastDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal TotalUnitsSoldLast7Days { get; set; }
    public decimal AverageDailySales { get; set; }
    public double EstimatedDaysRemaining { get; set; }
    public decimal TargetStock { get; set; }
    public decimal SuggestedOrderQuantity { get; set; }
    public StockRiskLevel RiskLevel { get; set; }
    public string RiskLevelName => RiskLevel.ToString();
    public bool HasSufficientData { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = "Forecasts are rule-based estimates based on recent 7-day sales velocity.";
}

public class PurchaseRecommendationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal AverageDailySales { get; set; }
    public double EstimatedDaysRemaining { get; set; }
    public decimal SuggestedOrderQuantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public decimal EstimatedPurchaseCost { get; set; }
    public StockRiskLevel RiskLevel { get; set; }
    public string RiskLevelName => RiskLevel.ToString();
    public string Reason { get; set; } = string.Empty;
}

public class AIAssistantRequestDto
{
    public string Question { get; set; } = string.Empty;
}

public class AIAssistantResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public List<string> SuggestedFollowUps { get; set; } = new();
    public bool IsAiGenerated { get; set; } = true;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class InvoiceItemExtractDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceExtractResponseDto
{
    public string? SupplierName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InvoiceItemExtractDto> Items { get; set; } = new();
    public string Notes { get; set; } = "Extracted via AI. Please verify before creating purchase.";
}
