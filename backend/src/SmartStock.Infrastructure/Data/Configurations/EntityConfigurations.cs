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
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => s.BusinessId);
        builder.HasIndex(s => s.SupplierName);

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

        builder.HasIndex(s => s.BusinessId);
        builder.HasIndex(s => s.SaleDate);
        builder.HasIndex(s => s.InvoiceNumber);

        builder.HasOne(s => s.Business)
            .WithMany(b => b.Sales)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);
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
