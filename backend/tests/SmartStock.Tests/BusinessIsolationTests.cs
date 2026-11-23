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

public class BusinessIsolationTests
{
    [Fact]
    public async Task CrossBusinessAccess_IsPrevented_AndIsolated()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();

        var businessAId = Guid.NewGuid();
        var businessBId = Guid.NewGuid();

        var businessA = new Business { Id = businessAId, BusinessName = "Store A", OwnerName = "Owner A" };
        var businessB = new Business { Id = businessBId, BusinessName = "Store B", OwnerName = "Owner B" };
        context.Businesses.AddRange(businessA, businessB);

        var catA = new Category { Id = Guid.NewGuid(), BusinessId = businessAId, Name = "Grocery Store A" };
        var catB = new Category { Id = Guid.NewGuid(), BusinessId = businessBId, Name = "Grocery Store B" };
        context.Categories.AddRange(catA, catB);

        var prodA = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessAId,
            CategoryId = catA.Id,
            Name = "Aashirvaad Atta 5kg",
            SKU = "ATTA-01",
            CurrentStock = 20,
            PurchasePrice = 200,
            SellingPrice = 240,
            Unit = UnitType.Packet
        };
        context.Products.Add(prodA);
        await context.SaveChangesAsync();

        // Simulate Authenticated User of Business B
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.GetRequiredBusinessId()).Returns(businessBId);
        currentUserServiceMock.Setup(s => s.BusinessId).Returns(businessBId);

        var productServiceForB = new ProductService(context, currentUserServiceMock.Object);
        var categoryServiceForB = new CategoryService(context, currentUserServiceMock.Object);

        // Act 1: Business B queries products
        var productsForB = await productServiceForB.GetPagedAsync(new ProductFilterParams());

        // Assert 1: Store B must NOT see Store A's products
        productsForB.Data!.Items.Should().BeEmpty();

        // Act 2: Business B attempts to access Store A's product by ID
        var actGetById = async () => await productServiceForB.GetByIdAsync(prodA.Id);
        await actGetById.Should().ThrowAsync<NotFoundException>();

        // Act 3: Business B queries categories
        var categoriesForB = await categoryServiceForB.GetAllAsync();
        categoriesForB.Data.Should().HaveCount(1);
        categoriesForB.Data![0].Name.Should().Be("Grocery Store B");

        // Act 4: Business B tries to create a product referencing Business A's category
        var createDto = new CreateProductDto
        {
            CategoryId = catA.Id, // Belonging to Business A!
            Name = "Malicious Product",
            SKU = "MAL-01",
            PurchasePrice = 50,
            SellingPrice = 60,
            CurrentStock = 10,
            MinimumStock = 5
        };
        var actCreate = async () => await productServiceForB.CreateAsync(createDto);
        await actCreate.Should().ThrowAsync<ValidationAppException>();
    }
}
