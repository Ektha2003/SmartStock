using SmartStock.Core.Enums;

namespace SmartStock.Core.Entities;

public class Sale : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public OrderSource OrderSource { get; set; } = OrderSource.Manual;
    public string? Notes { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

public class SaleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation properties
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class Purchase : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid? SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class StockTransaction : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class Notification : BaseEntity, IBusinessScopedEntity
{
    public Guid BusinessId { get; set; }
    public Guid? ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public bool IsRead { get; set; } = false;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Product? Product { get; set; }
}
