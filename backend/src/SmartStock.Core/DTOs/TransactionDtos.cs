using SmartStock.Core.Enums;

namespace SmartStock.Core.DTOs;

public class CreateSaleDto
{
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SaleDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
}

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public UnitType Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class SalesSummaryDto
{
    public decimal TotalSalesToday { get; set; }
    public decimal TotalSalesThisMonth { get; set; }
    public decimal TotalSalesAllTime { get; set; }
    public int TotalOrdersToday { get; set; }
    public int TotalOrdersThisMonth { get; set; }
}

public class CreatePurchaseDto
{
    public Guid? SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<CreatePurchaseItemDto> Items { get; set; } = new();
}

public class CreatePurchaseItemDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class PurchaseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SubTotal { get; set; }
}

public class PurchasesSummaryDto
{
    public decimal TotalPurchasesThisMonth { get; set; }
    public decimal TotalPurchasesAllTime { get; set; }
    public int TotalOrdersThisMonth { get; set; }
}

public class StockAdjustmentDto
{
    public Guid ProductId { get; set; }
    public decimal NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InventorySummaryDto
{
    public int TotalProducts { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public decimal TotalInventoryCostValue { get; set; }
    public decimal TotalInventoryRetailValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}
