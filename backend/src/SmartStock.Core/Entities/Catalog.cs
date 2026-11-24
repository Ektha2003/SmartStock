using SmartStock.Core.Enums;

namespace SmartStock.Core.Entities;

public class Category : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public UnitType Unit { get; set; } = UnitType.Piece;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Supplier : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<SupplierInvoice> Invoices { get; set; } = new List<SupplierInvoice>();
    public ICollection<ProductPurchasePriceHistory> PriceHistories { get; set; } = new List<ProductPurchasePriceHistory>();
}
