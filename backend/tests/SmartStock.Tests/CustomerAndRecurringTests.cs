using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class CustomerAndRecurringTests
{
    private (ApplicationDbContext Context, Guid BusinessId, Mock<ICurrentUserService> UserMock) SetupContext()
    {
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            BusinessName = "Sharma Kirana Store",
            OwnerName = "Rahul Sharma",
            IsActive = true
        };
        context.Businesses.Add(business);
        context.SaveChanges();

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.GetRequiredBusinessId()).Returns(businessId);
        userMock.Setup(u => u.BusinessId).Returns(businessId);

        return (context, businessId, userMock);
    }

    [Fact]
    public async Task CreateCustomer_InitializesDefaultRecurringSetting()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);

        var dto = new CreateCustomerDto
        {
            Name = "Rahul Sharma",
            Email = "rahul.sharma@example.com",
            Phone = "9876543210",
            BillingAddress = "B-12, Sector 5, Noida",
            DeliveryAddress = "B-12, Sector 5, Noida",
            RecurringOrderEnabled = true,
            DefaultDeliveryInstructions = "Ring doorbell twice"
        };

        var response = await customerService.CreateAsync(dto);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Rahul Sharma");
        response.Data.RecurringOrderEnabled.Should().BeTrue();
        response.Data.RecurringSetting.Should().NotBeNull();
        response.Data.RecurringSetting!.SubscriptionStatus.Should().Be(SubscriptionStatus.Active);
        response.Data.RecurringSetting.FrequencyType.Should().Be(RecurringFrequency.Every7Days);

        var savedInDb = await context.Customers
            .Include(c => c.RecurringSetting)
            .FirstOrDefaultAsync(c => c.Id == response.Data.Id);

        savedInDb.Should().NotBeNull();
        savedInDb!.RecurringSetting.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmailInSameBusiness_ThrowsConflict()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);

        var dto = new CreateCustomerDto
        {
            Name = "Amit Kumar",
            Email = "amit@example.com",
            Phone = "9998887770",
            BillingAddress = "Sector 18",
            DeliveryAddress = "Sector 18"
        };

        await customerService.CreateAsync(dto);

        // Attempt duplicate
        await Assert.ThrowsAsync<DuplicateEntityException>(() => customerService.CreateAsync(dto));
    }

    [Fact]
    public async Task AddRecurringProduct_RequiresExistingInventoryProduct()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);
        var recurringProductService = new CustomerRecurringProductService(context, userMock.Object);

        var customerRes = await customerService.CreateAsync(new CreateCustomerDto
        {
            Name = "Pooja Verma",
            Email = "pooja@example.com",
            Phone = "9876500000",
            DeliveryAddress = "Indirapuram"
        });

        var fakeProductId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundAppException>(() => recurringProductService.AddProductAsync(
            customerRes.Data!.Id,
            new CreateCustomerRecurringProductDto
            {
                ProductId = fakeProductId,
                DefaultQuantity = 5,
                Unit = UnitType.Kg
            }));
    }

    [Fact]
    public async Task GenerateDraftOrder_Idempotent_PreventsDuplicateForSameCycle()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);
        var recurringProductService = new CustomerRecurringProductService(context, userMock.Object);
        var salesServiceMock = new Mock<ISalesService>();
        var recurringOrderService = new RecurringOrderService(context, userMock.Object, salesServiceMock.Object);

        var category = new Category { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Staples" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Basmati Rice 5kg",
            SKU = "RICE-5KG",
            CurrentStock = 50,
            MinimumStock = 10,
            SellingPrice = 350,
            Unit = UnitType.Packet,
            IsActive = true
        };
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var customerRes = await customerService.CreateAsync(new CreateCustomerDto
        {
            Name = "Vikram Singh",
            Email = "vikram@example.com",
            Phone = "9811223344",
            DeliveryAddress = "Greater Noida",
            RecurringOrderEnabled = true
        });

        await recurringProductService.AddProductAsync(customerRes.Data!.Id, new CreateCustomerRecurringProductDto
        {
            ProductId = product.Id,
            DefaultQuantity = 2,
            Unit = UnitType.Packet
        });

        var scheduledDate = DateTime.UtcNow.Date;

        // Generate First
        var draft1 = await recurringOrderService.GenerateDraftOrderForCustomerAsync(customerRes.Data!.Id, scheduledDate);
        draft1.Success.Should().BeTrue();
        draft1.Data!.Items.Should().HaveCount(1);
        draft1.Data.TotalAmount.Should().Be(700); // 2 * 350

        // Generate Second for same date -> Returns existing without duplicate
        var draft2 = await recurringOrderService.GenerateDraftOrderForCustomerAsync(customerRes.Data!.Id, scheduledDate);
        draft2.Success.Should().BeTrue();
        draft2.Data!.Id.Should().Be(draft1.Data.Id);

        var totalOrdersInDb = await context.RecurringOrders.CountAsync(ro => ro.CustomerId == customerRes.Data.Id);
        totalOrdersInDb.Should().Be(1);
    }

    [Fact]
    public async Task CustomerPortal_ApproveAndModifyOrder_WithoutLogin()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);
        var recurringProductService = new CustomerRecurringProductService(context, userMock.Object);
        var salesServiceMock = new Mock<ISalesService>();
        var recurringOrderService = new RecurringOrderService(context, userMock.Object, salesServiceMock.Object);
        var portalService = new CustomerPortalService(context);

        var productRice = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = Guid.NewGuid(),
            Name = "Kolam Rice",
            SKU = "RICE-KOLAM",
            CurrentStock = 100,
            MinimumStock = 10,
            SellingPrice = 60,
            Unit = UnitType.Kg,
            IsActive = true
        };
        var productOil = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = Guid.NewGuid(),
            Name = "Sunflower Oil",
            SKU = "OIL-SUN",
            CurrentStock = 50,
            MinimumStock = 5,
            SellingPrice = 130,
            Unit = UnitType.Litre,
            IsActive = true
        };
        context.Products.AddRange(productRice, productOil);
        await context.SaveChangesAsync();

        var customerRes = await customerService.CreateAsync(new CreateCustomerDto
        {
            Name = "Sneha Patel",
            Email = "sneha@example.com",
            Phone = "9822334455",
            DeliveryAddress = "Navi Mumbai",
            RecurringOrderEnabled = true
        });

        await recurringProductService.AddProductAsync(customerRes.Data!.Id, new CreateCustomerRecurringProductDto
        {
            ProductId = productRice.Id,
            DefaultQuantity = 5,
            Unit = UnitType.Kg
        });

        var draft = await recurringOrderService.GenerateDraftOrderForCustomerAsync(customerRes.Data!.Id, DateTime.UtcNow);
        var tokenRecord = await context.RecurringOrderApprovalTokens.FirstAsync(t => t.RecurringOrderId == draft.Data!.Id);

        // Fetch via public portal using token
        var portalView = await portalService.GetOrderByTokenAsync(tokenRecord.Token);
        portalView.Success.Should().BeTrue();
        portalView.Data!.OrderNumber.Should().Be(draft.Data!.OrderNumber);
        portalView.Data.Items.Should().HaveCount(1);

        // Modify order: Increase rice to 10kg and add 2L oil
        var submitRes = await portalService.SubmitOrderDecisionAsync(tokenRecord.Token, new CustomerPortalSubmitDto
        {
            Action = "MODIFY",
            Items = new List<CustomerPortalItemUpdateDto>
            {
                new() { ProductId = productRice.Id, Quantity = 10 },
                new() { ProductId = productOil.Id, Quantity = 2 }
            }
        });

        submitRes.Success.Should().BeTrue();
        submitRes.Data!.Status.Should().Be(RecurringOrderStatus.Approved);
        submitRes.Data.Items.Should().HaveCount(2);
        submitRes.Data.TotalAmount.Should().Be((10 * 60) + (2 * 130)); // 600 + 260 = 860
    }

    [Fact]
    public async Task AIEmailReplyProcessor_ApproveAndSkipAndPermanentSubscription()
    {
        var (context, businessId, userMock) = SetupContext();
        var customerService = new CustomerService(context, userMock.Object);
        var recurringProductService = new CustomerRecurringProductService(context, userMock.Object);
        var salesServiceMock = new Mock<ISalesService>();
        var recurringOrderService = new RecurringOrderService(context, userMock.Object, salesServiceMock.Object);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = Guid.NewGuid(),
            Name = "Aashirvaad Atta 10kg",
            SKU = "ATTA-10KG",
            CurrentStock = 30,
            MinimumStock = 5,
            SellingPrice = 420,
            Unit = UnitType.Packet,
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var customerRes = await customerService.CreateAsync(new CreateCustomerDto
        {
            Name = "Anil Kapoor",
            Email = "anil@example.com",
            Phone = "9833445566",
            DeliveryAddress = "Andheri West, Mumbai",
            RecurringOrderEnabled = true
        });

        await recurringProductService.AddProductAsync(customerRes.Data!.Id, new CreateCustomerRecurringProductDto
        {
            ProductId = product.Id,
            DefaultQuantity = 1,
            Unit = UnitType.Packet
        });

        var draft = await recurringOrderService.GenerateDraftOrderForCustomerAsync(customerRes.Data!.Id, DateTime.UtcNow);

        var configMock = new Mock<IConfiguration>();
        var emailGenMock = new Mock<IAIEmailGeneratorService>();
        var emailServMock = new Mock<IEmailService>();

        var replyProcessor = new AIEmailReplyProcessor(
            context,
            configMock.Object,
            new HttpClient(),
            NullLogger<AIEmailReplyProcessor>.Instance,
            emailGenMock.Object,
            emailServMock.Object);

        // 1. Test "Yes, send me the same order." -> Approves order
        var reply1 = await replyProcessor.ProcessReplyAsync(new AIEmailReplyRequestDto
        {
            FromEmail = "anil@example.com",
            Subject = "Re: Upcoming Order",
            Body = "Yes, send me the same order."
        });

        reply1.Success.Should().BeTrue();
        reply1.Data!.Intent.Should().Be("APPROVE_ORDER");
        reply1.Data.RequiresManualReview.Should().BeFalse();

        var updatedOrder = await context.RecurringOrders.FindAsync(draft.Data!.Id);
        updatedOrder!.Status.Should().Be(RecurringOrderStatus.Approved);

        // Reset status for next test
        updatedOrder.Status = RecurringOrderStatus.PendingCustomerApproval;
        await context.SaveChangesAsync();

        // 2. Test "Please don't send anything this time." -> Skips order
        var reply2 = await replyProcessor.ProcessReplyAsync(new AIEmailReplyRequestDto
        {
            FromEmail = "anil@example.com",
            Subject = "Re: Upcoming Order",
            Body = "Please don't send anything this time."
        });

        reply2.Success.Should().BeTrue();
        reply2.Data!.Intent.Should().Be("SKIP_ORDER");
        updatedOrder = await context.RecurringOrders.FindAsync(draft.Data!.Id);
        updatedOrder!.Status.Should().Be(RecurringOrderStatus.Skipped);

        // 3. Test Permanent subscription change -> Flags for explicit customer confirmation
        var reply3 = await replyProcessor.ProcessReplyAsync(new AIEmailReplyRequestDto
        {
            FromEmail = "anil@example.com",
            Subject = "Re: Upcoming Order",
            Body = "Please increase my regular rice quantity to 15 kg for all future orders."
        });

        reply3.Success.Should().BeTrue();
        reply3.Data!.SubscriptionChange.Should().BeTrue();
        reply3.Data.RequiresConfirmation.Should().BeTrue();
        reply3.Data.RequiresManualReview.Should().BeTrue();
    }
}
