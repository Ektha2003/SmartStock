using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartStock.Core.Entities;

namespace SmartStock.Infrastructure.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BusinessName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.OwnerName).IsRequired().HasMaxLength(150);
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.BusinessType).HasMaxLength(100);
        builder.Property(b => b.IsActive).HasDefaultValue(true);

        builder.HasIndex(b => b.BusinessName);
        builder.HasIndex(b => b.CreatedAt);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.BusinessId);

        builder.HasOne(u => u.Business)
            .WithMany(b => b.Users)
            .HasForeignKey(u => u.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token).IsRequired().HasMaxLength(500);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Token);

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => new { c.BusinessId, c.Name }).IsUnique();
        builder.HasIndex(c => c.BusinessId);

        builder.HasOne(c => c.Business)
            .WithMany(b => b.Categories)
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.PurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 2);
        builder.Property(p => p.CurrentStock).HasPrecision(18, 3);
        builder.Property(p => p.MinimumStock).HasPrecision(18, 3);
        builder.Property(p => p.Unit).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => new { p.BusinessId, p.SKU }).IsUnique();
        builder.HasIndex(p => p.BusinessId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.CurrentStock);
        builder.HasIndex(p => p.CreatedAt);

        builder.HasOne(p => p.Business)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SupplierName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.CompanyName).HasMaxLength(200);
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.GstNumber).HasMaxLength(50);
        builder.Property(s => s.PanNumber).HasMaxLength(50);
        builder.Property(s => s.BillingAddress).HasMaxLength(500);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.PaymentTerms).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => s.BusinessId);
        builder.HasIndex(s => s.SupplierName);
        builder.HasIndex(s => s.GstNumber);

        builder.HasOne(s => s.Business)
            .WithMany(b => b.Suppliers)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.OrderSource).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(s => s.BusinessId);
        builder.HasIndex(s => s.SaleDate);
        builder.HasIndex(s => s.InvoiceNumber);
        builder.HasIndex(s => s.CustomerId);

        builder.HasOne(s => s.Business)
            .WithMany(b => b.Sales)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items");

        builder.HasKey(si => si.Id);

        builder.Property(si => si.Quantity).HasPrecision(18, 3);
        builder.Property(si => si.UnitPrice).HasPrecision(18, 2);
        builder.Property(si => si.SubTotal).HasPrecision(18, 2);

        builder.HasIndex(si => si.SaleId);
        builder.HasIndex(si => si.ProductId);

        builder.HasOne(si => si.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasIndex(p => p.BusinessId);
        builder.HasIndex(p => p.PurchaseDate);
        builder.HasIndex(p => p.SupplierId);

        builder.HasOne(p => p.Business)
            .WithMany(b => b.Purchases)
            .HasForeignKey(p => p.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Quantity).HasPrecision(18, 3);
        builder.Property(pi => pi.UnitCost).HasPrecision(18, 2);
        builder.Property(pi => pi.SubTotal).HasPrecision(18, 2);

        builder.HasIndex(pi => pi.PurchaseId);
        builder.HasIndex(pi => pi.ProductId);

        builder.HasOne(pi => pi.Purchase)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.PurchaseItems)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("stock_transactions");

        builder.HasKey(st => st.Id);

        builder.Property(st => st.TransactionType).HasConversion<string>().HasMaxLength(50);
        builder.Property(st => st.Quantity).HasPrecision(18, 3);
        builder.Property(st => st.PreviousStock).HasPrecision(18, 3);
        builder.Property(st => st.NewStock).HasPrecision(18, 3);
        builder.Property(st => st.Notes).HasMaxLength(1000);

        builder.HasIndex(st => st.BusinessId);
        builder.HasIndex(st => st.ProductId);
        builder.HasIndex(st => st.CreatedAt);
        builder.HasIndex(st => st.TransactionType);
        builder.HasIndex(st => st.ReferenceId);

        builder.HasOne(st => st.Business)
            .WithMany(b => b.StockTransactions)
            .HasForeignKey(st => st.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.Product)
            .WithMany(p => p.StockTransactions)
            .HasForeignKey(st => st.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.NotificationType).HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasIndex(n => n.BusinessId);
        builder.HasIndex(n => n.ProductId);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAt);

        builder.HasOne(n => n.Business)
            .WithMany(b => b.Notifications)
            .HasForeignKey(n => n.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Product)
            .WithMany(p => p.Notifications)
            .HasForeignKey(n => n.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.BillingAddress).HasMaxLength(500);
        builder.Property(c => c.DeliveryAddress).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.PreferredContactMethod).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.RecurringOrderEnabled).HasDefaultValue(false);
        builder.Property(c => c.DefaultDeliveryInstructions).HasMaxLength(1000);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => c.BusinessId);
        builder.HasIndex(c => new { c.BusinessId, c.Email }).IsUnique();
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.CreatedAt);

        builder.HasOne(c => c.Business)
            .WithMany(b => b.Customers)
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomerRecurringSettingConfiguration : IEntityTypeConfiguration<CustomerRecurringSetting>
{
    public void Configure(EntityTypeBuilder<CustomerRecurringSetting> builder)
    {
        builder.ToTable("customer_recurring_settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FrequencyType).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.PreferredDeliveryTime).HasMaxLength(100);
        builder.Property(s => s.TimeZone).HasMaxLength(100).HasDefaultValue("Asia/Kolkata");
        builder.Property(s => s.SubscriptionStatus).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(s => s.BusinessId);
        builder.HasIndex(s => s.CustomerId).IsUnique();
        builder.HasIndex(s => s.NextOrderDate);
        builder.HasIndex(s => s.SubscriptionStatus);

        builder.HasOne(s => s.Business)
            .WithMany()
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithOne(c => c.RecurringSetting)
            .HasForeignKey<CustomerRecurringSetting>(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomerRecurringProductConfiguration : IEntityTypeConfiguration<CustomerRecurringProduct>
{
    public void Configure(EntityTypeBuilder<CustomerRecurringProduct> builder)
    {
        builder.ToTable("customer_recurring_products");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.DefaultQuantity).HasPrecision(18, 3);
        builder.Property(rp => rp.Unit).HasConversion<string>().HasMaxLength(50);
        builder.Property(rp => rp.PriceOverride).HasPrecision(18, 2);
        builder.Property(rp => rp.MinQuantity).HasPrecision(18, 3);
        builder.Property(rp => rp.MaxQuantity).HasPrecision(18, 3);
        builder.Property(rp => rp.ProductInstructions).HasMaxLength(500);
        builder.Property(rp => rp.IsActive).HasDefaultValue(true);

        builder.HasIndex(rp => rp.BusinessId);
        builder.HasIndex(rp => new { rp.CustomerId, rp.ProductId }).IsUnique();

        builder.HasOne(rp => rp.Business)
            .WithMany()
            .HasForeignKey(rp => rp.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rp => rp.Customer)
            .WithMany(c => c.RecurringProducts)
            .HasForeignKey(rp => rp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Product)
            .WithMany()
            .HasForeignKey(rp => rp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecurringOrderConfiguration : IEntityTypeConfiguration<RecurringOrder>
{
    public void Configure(EntityTypeBuilder<RecurringOrder> builder)
    {
        builder.ToTable("recurring_orders");

        builder.HasKey(ro => ro.Id);

        builder.Property(ro => ro.OrderNumber).IsRequired().HasMaxLength(100);
        builder.Property(ro => ro.OrderSource).HasConversion<string>().HasMaxLength(50);
        builder.Property(ro => ro.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(ro => ro.PreferredDeliveryTime).HasMaxLength(100);
        builder.Property(ro => ro.SubTotal).HasPrecision(18, 2);
        builder.Property(ro => ro.TotalAmount).HasPrecision(18, 2);
        builder.Property(ro => ro.CustomerNotes).HasMaxLength(1000);
        builder.Property(ro => ro.SpecialInstructions).HasMaxLength(1000);
        builder.Property(ro => ro.ManualReviewReason).HasMaxLength(1000);

        builder.HasIndex(ro => ro.BusinessId);
        builder.HasIndex(ro => ro.CustomerId);
        builder.HasIndex(ro => ro.OrderNumber).IsUnique();
        builder.HasIndex(ro => ro.Status);
        builder.HasIndex(ro => ro.ScheduledOrderDate);
        builder.HasIndex(ro => ro.ExpectedDeliveryDate);
        builder.HasIndex(ro => ro.RequiresManualReview);

        builder.HasOne(ro => ro.Business)
            .WithMany(b => b.RecurringOrders)
            .HasForeignKey(ro => ro.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ro => ro.Customer)
            .WithMany(c => c.RecurringOrders)
            .HasForeignKey(ro => ro.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ro => ro.ConfirmedSale)
            .WithMany()
            .HasForeignKey(ro => ro.ConfirmedSaleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RecurringOrderItemConfiguration : IEntityTypeConfiguration<RecurringOrderItem>
{
    public void Configure(EntityTypeBuilder<RecurringOrderItem> builder)
    {
        builder.ToTable("recurring_order_items");

        builder.HasKey(roi => roi.Id);

        builder.Property(roi => roi.Quantity).HasPrecision(18, 3);
        builder.Property(roi => roi.OriginalQuantity).HasPrecision(18, 3);
        builder.Property(roi => roi.Unit).HasConversion<string>().HasMaxLength(50);
        builder.Property(roi => roi.UnitPrice).HasPrecision(18, 2);
        builder.Property(roi => roi.SubTotal).HasPrecision(18, 2);
        builder.Property(roi => roi.ProductInstructions).HasMaxLength(500);

        builder.HasIndex(roi => roi.RecurringOrderId);
        builder.HasIndex(roi => roi.ProductId);

        builder.HasOne(roi => roi.RecurringOrder)
            .WithMany(ro => ro.Items)
            .HasForeignKey(roi => roi.RecurringOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(roi => roi.Product)
            .WithMany()
            .HasForeignKey(roi => roi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecurringOrderApprovalTokenConfiguration : IEntityTypeConfiguration<RecurringOrderApprovalToken>
{
    public void Configure(EntityTypeBuilder<RecurringOrderApprovalToken> builder)
    {
        builder.ToTable("recurring_order_approval_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).IsRequired().HasMaxLength(256);
        builder.Property(t => t.ClientIp).HasMaxLength(64);

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.RecurringOrderId);
        builder.HasIndex(t => t.ExpiresAt);

        builder.HasOne(t => t.RecurringOrder)
            .WithMany(ro => ro.ApprovalTokens)
            .HasForeignKey(t => t.RecurringOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomerEmailLogConfiguration : IEntityTypeConfiguration<CustomerEmailLog>
{
    public void Configure(EntityTypeBuilder<CustomerEmailLog> builder)
    {
        builder.ToTable("customer_email_logs");

        builder.HasKey(el => el.Id);

        builder.Property(el => el.Direction).HasConversion<string>().HasMaxLength(50);
        builder.Property(el => el.Subject).IsRequired().HasMaxLength(300);
        builder.Property(el => el.FromEmail).IsRequired().HasMaxLength(256);
        builder.Property(el => el.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(el => el.MessageId).HasMaxLength(256);
        builder.Property(el => el.ThreadId).HasMaxLength(256);
        builder.Property(el => el.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(el => el.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(el => el.BusinessId);
        builder.HasIndex(el => el.CustomerId);
        builder.HasIndex(el => el.RecurringOrderId);
        builder.HasIndex(el => el.MessageId);
        builder.HasIndex(el => el.ThreadId);
        builder.HasIndex(el => el.CreatedAt);

        builder.HasOne(el => el.Business)
            .WithMany()
            .HasForeignKey(el => el.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(el => el.Customer)
            .WithMany(c => c.EmailLogs)
            .HasForeignKey(el => el.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(el => el.RecurringOrder)
            .WithMany(ro => ro.EmailLogs)
            .HasForeignKey(el => el.RecurringOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AIEmailProcessingLogConfiguration : IEntityTypeConfiguration<AIEmailProcessingLog>
{
    public void Configure(EntityTypeBuilder<AIEmailProcessingLog> builder)
    {
        builder.ToTable("ai_email_processing_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ParsedIntent).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ConfidenceScore).HasPrecision(5, 4);
        builder.Property(l => l.ManualReviewReason).HasMaxLength(1000);
        builder.Property(l => l.ValidationErrors).HasMaxLength(2000);
        builder.Property(l => l.ExecutionSummary).HasMaxLength(2000);

        builder.HasIndex(l => l.BusinessId);
        builder.HasIndex(l => l.CustomerId);
        builder.HasIndex(l => l.RecurringOrderId);
        builder.HasIndex(l => l.RequiresManualReview);
        builder.HasIndex(l => l.CreatedAt);

        builder.HasOne(l => l.Business)
            .WithMany()
            .HasForeignKey(l => l.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Customer)
            .WithMany(c => c.AIEmailLogs)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.RecurringOrder)
            .WithMany(ro => ro.AIEmailLogs)
            .HasForeignKey(l => l.RecurringOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.EmailLog)
            .WithMany()
            .HasForeignKey(l => l.EmailLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
    {
        builder.ToTable("supplier_invoices");

        builder.HasKey(si => si.Id);

        builder.Property(si => si.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(si => si.PaymentTerms).HasMaxLength(100);
        builder.Property(si => si.SupplierNameRaw).HasMaxLength(200);
        builder.Property(si => si.SupplierGst).HasMaxLength(50);
        builder.Property(si => si.BillingAddress).HasMaxLength(500);
        builder.Property(si => si.DeliveryAddress).HasMaxLength(500);
        builder.Property(si => si.PurchaseOrderNumber).HasMaxLength(100);

        builder.Property(si => si.SubTotal).HasPrecision(18, 2);
        builder.Property(si => si.Discount).HasPrecision(18, 2);
        builder.Property(si => si.TaxableAmount).HasPrecision(18, 2);
        builder.Property(si => si.Cgst).HasPrecision(18, 2);
        builder.Property(si => si.Sgst).HasPrecision(18, 2);
        builder.Property(si => si.Igst).HasPrecision(18, 2);
        builder.Property(si => si.OtherCharges).HasPrecision(18, 2);
        builder.Property(si => si.GrandTotal).HasPrecision(18, 2);

        builder.Property(si => si.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(si => si.FilePath).IsRequired().HasMaxLength(1000);
        builder.Property(si => si.ContentType).HasMaxLength(100);
        builder.Property(si => si.DocumentHash).HasMaxLength(128);
        builder.Property(si => si.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(si => si.DuplicateReason).HasMaxLength(500);
        builder.Property(si => si.Notes).HasMaxLength(2000);
        builder.Property(si => si.ApprovedByUserName).HasMaxLength(150);

        builder.HasIndex(si => si.BusinessId);
        builder.HasIndex(si => si.SupplierId);
        builder.HasIndex(si => new { si.BusinessId, si.InvoiceNumber });
        builder.HasIndex(si => new { si.BusinessId, si.DocumentHash });
        builder.HasIndex(si => si.Status);
        builder.HasIndex(si => si.InvoiceDate);
        builder.HasIndex(si => si.CreatedAt);

        builder.HasOne(si => si.Business)
            .WithMany(b => b.SupplierInvoices)
            .HasForeignKey(si => si.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(si => si.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(si => si.ConfirmedPurchase)
            .WithMany()
            .HasForeignKey(si => si.ConfirmedPurchaseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SupplierInvoiceItemConfiguration : IEntityTypeConfiguration<SupplierInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceItem> builder)
    {
        builder.ToTable("supplier_invoice_items");

        builder.HasKey(sii => sii.Id);

        builder.Property(sii => sii.RawProductName).IsRequired().HasMaxLength(250);
        builder.Property(sii => sii.RawDescription).HasMaxLength(500);
        builder.Property(sii => sii.RawSupplierSku).HasMaxLength(100);
        builder.Property(sii => sii.RawHsnCode).HasMaxLength(50);
        builder.Property(sii => sii.InvoiceUnit).HasMaxLength(50);

        builder.Property(sii => sii.InvoiceQuantity).HasPrecision(18, 3);
        builder.Property(sii => sii.InvoiceUnitPrice).HasPrecision(18, 2);
        builder.Property(sii => sii.Discount).HasPrecision(18, 2);
        builder.Property(sii => sii.TaxPercentage).HasPrecision(5, 2);
        builder.Property(sii => sii.TaxAmount).HasPrecision(18, 2);
        builder.Property(sii => sii.LineTotal).HasPrecision(18, 2);

        builder.Property(sii => sii.MatchStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(sii => sii.MatchConfidence).HasPrecision(5, 4);
        builder.Property(sii => sii.MatchExplanation).HasMaxLength(500);

        builder.Property(sii => sii.ConversionFactor).HasPrecision(18, 3);
        builder.Property(sii => sii.PackSize).HasPrecision(18, 3);
        builder.Property(sii => sii.CalculatedStockQuantity).HasPrecision(18, 3);
        builder.Property(sii => sii.TargetUnit).HasConversion<string>().HasMaxLength(50);

        builder.Property(sii => sii.PreviousPurchasePrice).HasPrecision(18, 2);
        builder.Property(sii => sii.NewPurchasePrice).HasPrecision(18, 2);
        builder.Property(sii => sii.PriceDifference).HasPrecision(18, 2);
        builder.Property(sii => sii.PercentagePriceChange).HasPrecision(7, 2);

        builder.HasIndex(sii => sii.SupplierInvoiceId);
        builder.HasIndex(sii => sii.MatchedProductId);
        builder.HasIndex(sii => sii.MatchStatus);

        builder.HasOne(sii => sii.SupplierInvoice)
            .WithMany(si => si.Items)
            .HasForeignKey(sii => sii.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sii => sii.MatchedProduct)
            .WithMany()
            .HasForeignKey(sii => sii.MatchedProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductPurchasePriceHistoryConfiguration : IEntityTypeConfiguration<ProductPurchasePriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPurchasePriceHistory> builder)
    {
        builder.ToTable("product_purchase_price_histories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.OldPrice).HasPrecision(18, 2);
        builder.Property(h => h.NewPrice).HasPrecision(18, 2);
        builder.Property(h => h.Notes).HasMaxLength(1000);

        builder.HasIndex(h => h.BusinessId);
        builder.HasIndex(h => h.ProductId);
        builder.HasIndex(h => h.SupplierId);
        builder.HasIndex(h => h.SupplierInvoiceId);
        builder.HasIndex(h => h.EffectiveDate);

        builder.HasOne(h => h.Business)
            .WithMany()
            .HasForeignKey(h => h.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Product)
            .WithMany()
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Supplier)
            .WithMany(s => s.PriceHistories)
            .HasForeignKey(h => h.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.SupplierInvoice)
            .WithMany()
            .HasForeignKey(h => h.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AIInvoiceProcessingLogConfiguration : IEntityTypeConfiguration<AIInvoiceProcessingLog>
{
    public void Configure(EntityTypeBuilder<AIInvoiceProcessingLog> builder)
    {
        builder.ToTable("ai_invoice_processing_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Stage).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ConfidenceScore).HasPrecision(5, 4);
        builder.Property(l => l.ExecutionSummary).HasMaxLength(2000);
        builder.Property(l => l.Errors).HasMaxLength(2000);

        builder.HasIndex(l => l.BusinessId);
        builder.HasIndex(l => l.SupplierInvoiceId);
        builder.HasIndex(l => l.Stage);
        builder.HasIndex(l => l.CreatedAt);

        builder.HasOne(l => l.Business)
            .WithMany()
            .HasForeignKey(l => l.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.SupplierInvoice)
            .WithMany(si => si.ProcessingLogs)
            .HasForeignKey(l => l.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
