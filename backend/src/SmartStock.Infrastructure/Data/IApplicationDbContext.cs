using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Entities;

namespace SmartStock.Infrastructure.Data;

public interface IApplicationDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerRecurringSetting> CustomerRecurringSettings { get; }
    DbSet<CustomerRecurringProduct> CustomerRecurringProducts { get; }
    DbSet<RecurringOrder> RecurringOrders { get; }
    DbSet<RecurringOrderItem> RecurringOrderItems { get; }
    DbSet<RecurringOrderApprovalToken> RecurringOrderApprovalTokens { get; }
    DbSet<CustomerEmailLog> CustomerEmailLogs { get; }
    DbSet<AIEmailProcessingLog> AIEmailProcessingLogs { get; }
    DbSet<SupplierInvoice> SupplierInvoices { get; }
    DbSet<SupplierInvoiceItem> SupplierInvoiceItems { get; }
    DbSet<ProductPurchasePriceHistory> ProductPurchasePriceHistories { get; }
    DbSet<AIInvoiceProcessingLog> AIInvoiceProcessingLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
