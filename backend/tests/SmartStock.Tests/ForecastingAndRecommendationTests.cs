using FluentAssertions;
using Moq;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Forecasting;
using Xunit;

namespace SmartStock.Tests;

public class ForecastingAndRecommendationTests
{
    [Fact]
    public async Task Forecasting_CalculatesVelocity_DaysRemaining_AndSuggestedOrder()
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
            Name = "Aashirvaad Atta 5kg",
            SKU = "GRO-ATT-01",
            CurrentStock = 14, // 14 units currently in stock
            MinimumStock = 10,
            PurchasePrice = 200,
            SellingPrice = 240,
            Unit = UnitType.Packet,
            IsActive = true
        };

        context.Categories.Add(category);
        context.Products.Add(product);

        // Generate sales during last 7 days: 49 units sold over 7 days -> Average Daily Sales = 49 / 7 = 7 units/day
        // Days remaining = 14 / 7 = 2.0 days (Critical risk!)
        // Target stock (10 days) = 7 * 10 = 70 units
        // Suggested order = max(0, 70 - 14) = 56 units
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceNumber = "INV-TEST-001",
            SaleDate = DateTime.UtcNow.AddDays(-3),
            TotalAmount = 49 * 240
        };

        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = product.Id,
            Quantity = 49,
            UnitPrice = 240,
            SubTotal = 49 * 240
        };

        sale.Items.Add(saleItem);
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var forecastingService = new ForecastingService(context, currentUserServiceMock.Object);

        // Act
        var result = await forecastingService.GetProductForecastAsync(product.Id);

        // Assert
        result.Success.Should().BeTrue();
        var forecast = result.Data!;
        forecast.AverageDailySales.Should().Be(7.0m);
        forecast.EstimatedDaysRemaining.Should().Be(2.0);
        forecast.TargetStock.Should().Be(70.0m);
        forecast.SuggestedOrderQuantity.Should().Be(56.0m);
        forecast.RiskLevel.Should().Be(StockRiskLevel.Critical);
        forecast.HasSufficientData.Should().BeTrue();
    }

    [Fact]
    public async Task Forecasting_ZeroSalesHistory_HandlesGracefullyWithoutDivisionByZero()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Household" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Colin Cleaner 500ml",
            SKU = "HH-CLN-01",
            CurrentStock = 20,
            MinimumStock = 5,
            PurchasePrice = 80,
            SellingPrice = 100,
            Unit = UnitType.Piece,
            IsActive = true
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var forecastingService = new ForecastingService(context, currentUserServiceMock.Object);

        // Act
        var result = await forecastingService.GetProductForecastAsync(product.Id);

        // Assert
        result.Success.Should().BeTrue();
        var forecast = result.Data!;
        forecast.AverageDailySales.Should().Be(0);
        forecast.EstimatedDaysRemaining.Should().Be(999);
        forecast.HasSufficientData.Should().BeFalse();
        forecast.Explanation.Should().ContainEquivalentOf("No sales in the last 7 days");
    }

    [Fact]
    public async Task PurchaseRecommendations_ReturnsActionableRestockSuggestions()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Dairy" };

        var prodCritical = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Amul Butter 500g",
            SKU = "DRY-BUT-01",
            CurrentStock = 2,
            MinimumStock = 10,
            PurchasePrice = 240,
            SellingPrice = 275,
            IsActive = true
        };

        var prodHealthy = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Amul Milk 1L",
            SKU = "DRY-MLK-01",
            CurrentStock = 100,
            MinimumStock = 15,
            PurchasePrice = 60,
            SellingPrice = 70,
            IsActive = true
        };

        context.Categories.Add(category);
        context.Products.AddRange(prodCritical, prodHealthy);

        // Add 14 units sold for Amul Butter over last 7 days -> 2/day velocity -> 2 units left = 1 day remaining!
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvoiceNumber = "INV-0099",
            SaleDate = DateTime.UtcNow.AddDays(-2),
            TotalAmount = 14 * 275
        };
        sale.Items.Add(new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = prodCritical.Id,
            Quantity = 14,
            UnitPrice = 275,
            SubTotal = 14 * 275
        });

        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessId);

        var forecastingService = new ForecastingService(context, currentUserServiceMock.Object);

        // Act
        var recsResult = await forecastingService.GetPurchaseRecommendationsAsync();

        // Assert
        recsResult.Success.Should().BeTrue();
        recsResult.Data.Should().NotBeNull();
        recsResult.Data!.Should().Contain(r => r.ProductId == prodCritical.Id);

        var butterRec = recsResult.Data.First(r => r.ProductId == prodCritical.Id);
        butterRec.SuggestedOrderQuantity.Should().BeGreaterThan(0);
        butterRec.EstimatedPurchaseCost.Should().Be(butterRec.SuggestedOrderQuantity * prodCritical.PurchasePrice);
        butterRec.RiskLevel.Should().Be(StockRiskLevel.Critical);
        butterRec.Reason.Should().NotBeNullOrWhiteSpace();
    }
}
