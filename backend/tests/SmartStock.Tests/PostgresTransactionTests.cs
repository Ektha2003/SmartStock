using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class PostgresTransactionTests
{
    [Fact]
    public async Task Postgres_SaleTransaction_CommitsAtomically()
    {
        // Real PostgreSQL test database
        using var context = TestDbContextFactory.CreatePostgresDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            BusinessName = "PG Test Kirana",
            OwnerName = "PG Owner",
            IsActive = true
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = $"PG Grocery {Guid.NewGuid():N}"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Tata Tea Gold 250g",
            SKU = $"TEA-{Guid.NewGuid():N}",
            CurrentStock = 20,
            MinimumStock = 5,
            PurchasePrice = 120,
            SellingPrice = 145,
            Unit = UnitType.Packet,
            IsActive = true
        };

        context.Businesses.Add(business);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var salesService = new SalesService(context, currentUserServiceMock.Object);

        var saleDto = new CreateSaleDto
        {
            SaleDate = DateTime.UtcNow,
            Notes = "PostgreSQL atomic sale test",
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = product.Id, Quantity = 5, UnitPrice = 145 }
            }
        };

        // Act
        var result = await salesService.CreateSaleAsync(saleDto);

        // Assert
        result.Success.Should().BeTrue();

        // Fresh context query to verify database commit
        using var verifyContext = TestDbContextFactory.CreatePostgresDbContext();
        var productInDb = await verifyContext.Products.FindAsync(product.Id);
        productInDb!.CurrentStock.Should().Be(15); // 20 - 5 = 15

        var saleInDb = await verifyContext.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == result.Data!.Id);
        saleInDb.Should().NotBeNull();
        saleInDb!.TotalAmount.Should().Be(5 * 145);
        saleInDb.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Postgres_FailedSale_RollsBackChangesCompletely()
    {
        // Real PostgreSQL test database
        using var context = TestDbContextFactory.CreatePostgresDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            BusinessName = "PG Rollback Store",
            OwnerName = "PG Rollback Owner",
            IsActive = true
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = $"PG Category {Guid.NewGuid():N}"
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "India Gate Basmati Rice 5kg",
            SKU = $"RIC-{Guid.NewGuid():N}",
            CurrentStock = 8,
            MinimumStock = 5,
            PurchasePrice = 400,
            SellingPrice = 480,
            Unit = UnitType.Packet,
            IsActive = true
        };

        context.Businesses.Add(business);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var salesService = new SalesService(context, currentUserServiceMock.Object);

        // Attempt to sell 15 units when only 8 are in stock
        var saleDto = new CreateSaleDto
        {
            SaleDate = DateTime.UtcNow,
            Notes = "Should fail and roll back",
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = product.Id, Quantity = 15, UnitPrice = 480 }
            }
        };

        // Act & Assert
        var act = async () => await salesService.CreateSaleAsync(saleDto);
        await act.Should().ThrowAsync<InsufficientStockException>();

        // Fresh context query to verify rollback in PostgreSQL
        using var verifyContext = TestDbContextFactory.CreatePostgresDbContext();
        var productInDb = await verifyContext.Products.FindAsync(product.Id);
        productInDb!.CurrentStock.Should().Be(8); // Unchanged!

        var salesCount = await verifyContext.Sales.CountAsync(s => s.BusinessId == businessId);
        salesCount.Should().Be(0); // Nothing committed
    }
}
