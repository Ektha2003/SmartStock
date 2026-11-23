using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Core.Validation;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class ProductAndValidationTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void CreateProductValidator_WithValidData_ShouldNotHaveErrors()
    {
        var model = new CreateProductDto
        {
            Name = "Tata Tea Premium 250g",
            SKU = "BEV-TEA-101",
            CategoryId = Guid.NewGuid(),
            PurchasePrice = 110,
            SellingPrice = 135,
            CurrentStock = 50,
            MinimumStock = 15,
            Unit = UnitType.Packet
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateProductValidator_WithNegativePriceOrStock_ShouldHaveErrors()
    {
        var model = new CreateProductDto
        {
            Name = "",
            SKU = "",
            CategoryId = Guid.Empty,
            PurchasePrice = -50,
            SellingPrice = -20,
            CurrentStock = -5,
            MinimumStock = -10
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.SKU);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice);
        result.ShouldHaveValidationErrorFor(x => x.SellingPrice);
        result.ShouldHaveValidationErrorFor(x => x.CurrentStock);
        result.ShouldHaveValidationErrorFor(x => x.MinimumStock);
    }

    [Fact]
    public async Task CreateProductAsync_Valid_CreatesProductAndAuditsOpeningStock()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business { Id = businessId, BusinessName = "Delhi Mart" };
        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Beverages" };
        context.Businesses.Add(business);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var productService = new ProductService(context, currentUserServiceMock.Object);

        var dto = new CreateProductDto
        {
            CategoryId = category.Id,
            Name = "Bru Instant Coffee 100g",
            SKU = "BEV-COF-201",
            PurchasePrice = 160,
            SellingPrice = 190,
            CurrentStock = 25,
            MinimumStock = 10,
            Unit = UnitType.Piece
        };

        // Act
        var result = await productService.CreateAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Bru Instant Coffee 100g");
        result.Data.CurrentStock.Should().Be(25);

        // Verify StockTransaction was recorded
        var transactions = context.StockTransactions
            .Where(st => st.ProductId == result.Data.Id)
            .ToList();

        transactions.Should().HaveCount(1);
        transactions[0].TransactionType.Should().Be(TransactionType.Opening);
        transactions[0].Quantity.Should().Be(25);
        transactions[0].NewStock.Should().Be(25);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsOnlyProductsAtOrBelowThreshold()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Dairy" };
        context.Categories.Add(category);

        var prodLow = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Amul Butter 500g",
            SKU = "DRY-BUT-99",
            CurrentStock = 3,
            MinimumStock = 10, // LOW STOCK
            IsActive = true
        };

        var prodHealthy = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Amul Milk 1L",
            SKU = "DRY-MLK-99",
            CurrentStock = 40,
            MinimumStock = 10, // HEALTHY
            IsActive = true
        };

        var prodOut = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Tata Salt 1kg",
            SKU = "GRO-SLT-99",
            CurrentStock = 0,
            MinimumStock = 15, // OUT OF STOCK
            IsActive = true
        };

        context.Products.AddRange(prodLow, prodHealthy, prodOut);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var productService = new ProductService(context, currentUserServiceMock.Object);

        // Act
        var lowStockResult = await productService.GetLowStockProductsAsync();

        // Assert
        lowStockResult.Success.Should().BeTrue();
        lowStockResult.Data.Should().HaveCount(2); // Amul Butter and Tata Salt
        lowStockResult.Data!.Select(p => p.SKU).Should().Contain(["DRY-BUT-99", "GRO-SLT-99"]);
        lowStockResult.Data.Select(p => p.SKU).Should().NotContain("DRY-MLK-99");
    }
}
