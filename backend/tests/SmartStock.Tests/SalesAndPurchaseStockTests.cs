using FluentAssertions;
using Moq;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class SalesAndPurchaseStockTests
{
    [Fact]
    public async Task Purchase_IncreasesStock_AndCreatesStockTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Grocery" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Fortune Oil 1L",
            SKU = "OIL-01",
            CurrentStock = 10,
            PurchasePrice = 120,
            SellingPrice = 140,
            MinimumStock = 5,
            Unit = UnitType.Piece
        };

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = "Delhi Wholesale Distributor"
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var purchaseService = new PurchaseService(context, currentUserServiceMock.Object);

        var purchaseDto = new CreatePurchaseDto
        {
            SupplierId = supplier.Id,
            PurchaseDate = DateTime.UtcNow,
            Notes = "Weekly replenishment",
            Items = new List<CreatePurchaseItemDto>
            {
                new() { ProductId = product.Id, Quantity = 20, UnitCost = 122 }
            }
        };

        // Act
        var result = await purchaseService.CreatePurchaseAsync(purchaseDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(20 * 122);

        // Verify product stock increased
        var updatedProduct = context.Products.Find(product.Id);
        updatedProduct!.CurrentStock.Should().Be(30); // 10 + 20
        updatedProduct.PurchasePrice.Should().Be(122); // Updated to latest cost

        // Verify StockTransaction
        var transactions = context.StockTransactions
            .Where(st => st.ProductId == product.Id && st.TransactionType == TransactionType.Purchase)
            .ToList();

        transactions.Should().HaveCount(1);
        transactions[0].Quantity.Should().Be(20);
        transactions[0].PreviousStock.Should().Be(10);
        transactions[0].NewStock.Should().Be(30);
    }

    [Fact]
    public async Task Sale_DecreasesStock_AndCreatesStockTransaction()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Snacks" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Parle-G Biscuits",
            SKU = "SNK-01",
            CurrentStock = 50,
            PurchasePrice = 20,
            SellingPrice = 25,
            MinimumStock = 10,
            Unit = UnitType.Packet
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var salesService = new SalesService(context, currentUserServiceMock.Object);

        var saleDto = new CreateSaleDto
        {
            SaleDate = DateTime.UtcNow,
            Notes = "Counter customer",
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = product.Id, Quantity = 12, UnitPrice = 25 }
            }
        };

        // Act
        var result = await salesService.CreateSaleAsync(saleDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(12 * 25);

        // Verify product stock decreased
        var updatedProduct = context.Products.Find(product.Id);
        updatedProduct!.CurrentStock.Should().Be(38); // 50 - 12

        // Verify StockTransaction
        var transactions = context.StockTransactions
            .Where(st => st.ProductId == product.Id && st.TransactionType == TransactionType.Sale)
            .ToList();

        transactions.Should().HaveCount(1);
        transactions[0].Quantity.Should().Be(12);
        transactions[0].PreviousStock.Should().Be(50);
        transactions[0].NewStock.Should().Be(38);
    }

    [Fact]
    public async Task Sale_InsufficientStock_ThrowsException_AndLeavesStockUnchanged()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Dairy" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Amul Paneer 200g",
            SKU = "PAN-01",
            CurrentStock = 5,
            PurchasePrice = 75,
            SellingPrice = 90,
            MinimumStock = 5,
            Unit = UnitType.Packet
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var salesService = new SalesService(context, currentUserServiceMock.Object);

        var saleDto = new CreateSaleDto
        {
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = product.Id, Quantity = 10, UnitPrice = 90 } // Requests 10, only 5 available!
            }
        };

        // Act & Assert
        var act = async () => await salesService.CreateSaleAsync(saleDto);
        await act.Should().ThrowAsync<InsufficientStockException>();

        // Stock must remain unchanged
        var productAfterFailedSale = context.Products.Find(product.Id);
        productAfterFailedSale!.CurrentStock.Should().Be(5);

        // No Sale record or StockTransaction should be saved
        context.Sales.Count().Should().Be(0);
        context.StockTransactions.Count().Should().Be(0);
    }

    [Fact]
    public async Task StockAdjustment_WithMandatoryReason_UpdatesStockAndCreatesAudit()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Grocery" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Sugar 1kg",
            SKU = "SUG-01",
            CurrentStock = 40,
            PurchasePrice = 38,
            SellingPrice = 44,
            MinimumStock = 10,
            Unit = UnitType.Packet
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var inventoryService = new InventoryService(context, currentUserServiceMock.Object);

        var adjDto = new StockAdjustmentDto
        {
            ProductId = product.Id,
            NewQuantity = 35,
            Reason = "Physical audit found 5 damaged water-leaked packets"
        };

        // Act
        var result = await inventoryService.AdjustStockAsync(adjDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.NewStock.Should().Be(35);
        result.Data.PreviousStock.Should().Be(40);
        result.Data.Quantity.Should().Be(5);
        result.Data!.Notes.Should().ContainEquivalentOf("physical audit");

        var productInDb = context.Products.Find(product.Id);
        productInDb!.CurrentStock.Should().Be(35);
    }
}
