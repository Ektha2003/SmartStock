using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.AI;
using SmartStock.Infrastructure.Data;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class SupplierInvoiceTests
{
    private (ApplicationDbContext Context, Guid BusinessId, Mock<ICurrentUserService> UserMock, Category Category, Product Product1, Product Product2) SetupContext()
    {
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            BusinessName = "SuperMart India",
            OwnerName = "Vikram Patel",
            IsActive = true
        };
        context.Businesses.Add(business);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Grains & Flours",
            Description = "Staples"
        };
        context.Categories.Add(category);

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Premium Basmati Rice",
            SKU = "BASMATI-001",
            Description = "Aromatic 5kg pack",
            PurchasePrice = 380.00m,
            SellingPrice = 490.00m,
            CurrentStock = 50.0m,
            MinimumStock = 10.0m,
            Unit = UnitType.Kg,
            IsActive = true
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = category.Id,
            Name = "Fortune Sunflower Oil",
            SKU = "FORTUNE-OIL-1L",
            Description = "Cooking oil",
            PurchasePrice = 120.00m,
            SellingPrice = 145.00m,
            CurrentStock = 25.0m,
            MinimumStock = 5.0m,
            Unit = UnitType.Litre,
            IsActive = true
        };

        context.Products.AddRange(product1, product2);
        context.SaveChanges();

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.GetRequiredBusinessId()).Returns(businessId);
        userMock.Setup(u => u.BusinessId).Returns(businessId);
        userMock.Setup(u => u.Email).Returns("admin@supermart.com");

        return (context, businessId, userMock, category, product1, product2);
    }

    [Fact]
    public void ProductMatchingEngine_MatchesExactSku()
    {
        var (_, _, _, _, product1, _) = SetupContext();
        var catalog = new List<Product> { product1 };

        var result = ProductMatchingEngine.EvaluateMatch(
            rawProductName: "Different Name Basmati",
            rawDescription: null,
            rawSupplierSku: "BASMATI-001",
            rawHsnCode: "100630",
            invoiceQuantity: 10,
            invoiceUnit: "PCS",
            invoiceUnitPrice: 400.00m,
            catalogProducts: catalog);

        result.Status.Should().Be(ProductMatchStatus.ExactMatch);
        result.Confidence.Should().Be(1.0m);
        result.MatchedProduct.Should().NotBeNull();
        result.MatchedProduct!.Id.Should().Be(product1.Id);
    }

    [Fact]
    public void ProductMatchingEngine_RecognizesPackSizeConversion()
    {
        var (_, _, _, _, product1, _) = SetupContext();
        var catalog = new List<Product> { product1 };

        // "Basmati Rice Premium - 5kg Pack" -> Product is in KG
        // 20 PCS x 5 KG = 100 KG
        var result = ProductMatchingEngine.EvaluateMatch(
            rawProductName: "Basmati Rice Premium - 5kg Pack",
            rawDescription: "Long grain pack",
            rawSupplierSku: null,
            rawHsnCode: null,
            invoiceQuantity: 20,
            invoiceUnit: "PCS",
            invoiceUnitPrice: 410.00m,
            catalogProducts: catalog);

        result.Status.Should().BeOneOf(ProductMatchStatus.HighConfidenceMatch, ProductMatchStatus.ExactMatch);
        result.ConversionFactor.Should().Be(5.0m);
        result.PackSize.Should().Be(5.0m);
        result.CalculatedStockQuantity.Should().Be(100.0m); // 20 x 5 = 100 KG
    }

    [Fact]
    public async Task SupplierService_CreatesAndRetrievesExtendedFields()
    {
        var (context, _, userMock, _, _, _) = SetupContext();
        var supplierService = new SupplierService(context, userMock.Object);

        var createDto = new CreateSupplierDto
        {
            SupplierName = "Metro Wholesale India",
            CompanyName = "Metro Cash & Carry India Pvt Ltd",
            ContactPerson = "Anil Kapoor",
            Phone = "9876543210",
            Email = "anil@metro.in",
            GstNumber = "27AAACM1234F1Z9",
            PanNumber = "AAACM1234F",
            BillingAddress = "Plot 42, Bandra Complex, Mumbai",
            Address = "Warehouse 5, Mumbai",
            PaymentTerms = "Net 30 Days",
            Notes = "Primary staples supplier"
        };

        var response = await supplierService.CreateAsync(createDto);
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.GstNumber.Should().Be("27AAACM1234F1Z9");
        response.Data.CompanyName.Should().Be("Metro Cash & Carry India Pvt Ltd");
        response.Data.PaymentTerms.Should().Be("Net 30 Days");

        var detailResponse = await supplierService.GetDetailExtendedAsync(response.Data.Id);
        detailResponse.Success.Should().BeTrue();
        detailResponse.Data!.SupplierName.Should().Be("Metro Wholesale India");
    }

    [Fact]
    public async Task InvoiceUpload_DetectsDuplicateHash()
    {
        var (context, businessId, userMock, _, _, _) = SetupContext();

        var scannerMock = new Mock<IInvoiceScannerService>();
        scannerMock.Setup(s => s.ExtractInvoiceAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<InvoiceExtractResponseDto>.Ok(new InvoiceExtractResponseDto
            {
                SupplierName = "Metro Wholesale",
                InvoiceNumber = "INV-2026-001",
                TotalAmount = 5000.00m,
                Items = new List<InvoiceItemExtractDto>()
            }));

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        var invoiceService = new SupplierInvoiceService(
            context,
            userMock.Object,
            scannerMock.Object,
            envMock.Object,
            NullLogger<SupplierInvoiceService>.Instance);

        var fileBytes = Encoding.UTF8.GetBytes("Sample invoice file content for testing");

        // First upload
        using var stream1 = new MemoryStream(fileBytes);
        var res1 = await invoiceService.UploadAndProcessInvoiceAsync(stream1, "invoice1.pdf", "application/pdf");
        res1.Success.Should().BeTrue();
        res1.Data!.IsDuplicate.Should().BeFalse();
        res1.Data.Status.Should().Be(SupplierInvoiceStatus.ReadyForReview);

        // Second upload of identical file bytes -> should flag as Duplicate
        using var stream2 = new MemoryStream(fileBytes);
        var res2 = await invoiceService.UploadAndProcessInvoiceAsync(stream2, "invoice1_copy.pdf", "application/pdf");
        res2.Success.Should().BeTrue();
        res2.Data!.IsDuplicate.Should().BeTrue();
        res2.Data.Status.Should().Be(SupplierInvoiceStatus.Duplicate);
        res2.Data.DuplicateReason.Should().Contain("Duplicate file detected");
    }

    [Fact]
    public async Task ApproveInvoice_UpdatesStock_LogsPriceHistory_CreatesPurchase()
    {
        var (context, businessId, userMock, category, product1, _) = SetupContext();

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = "Apex Food Distribution",
            IsActive = true
        };
        context.Suppliers.Add(supplier);

        var initialStock = product1.CurrentStock; // 50 KG
        var initialPrice = product1.PurchasePrice; // 380.00

        var invoice = new SupplierInvoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierId = supplier.Id,
            InvoiceNumber = "APEX-INV-101",
            InvoiceDate = DateTime.UtcNow,
            SubTotal = 8200.00m,
            GrandTotal = 8200.00m,
            OriginalFileName = "apex_invoice.pdf",
            FilePath = "/uploads/invoices/test.pdf",
            Status = SupplierInvoiceStatus.ReadyForReview,
            CreatedAt = DateTime.UtcNow
        };

        // Item: 20 PCS of 5KG pack = 100 KG. Price changed from 380 to 410 (+30)
        var item = new SupplierInvoiceItem
        {
            Id = Guid.NewGuid(),
            SupplierInvoiceId = invoice.Id,
            RawProductName = "Premium Basmati Rice 5kg",
            InvoiceQuantity = 20,
            InvoiceUnit = "PCS",
            InvoiceUnitPrice = 410.00m,
            LineTotal = 8200.00m,
            MatchedProductId = product1.Id,
            MatchStatus = ProductMatchStatus.UserConfirmed,
            ConversionFactor = 5.0m,
            PackSize = 5.0m,
            CalculatedStockQuantity = 100.0m, // 20 * 5 = 100 KG
            PreviousPurchasePrice = initialPrice,
            NewPurchasePrice = 410.00m,
            PriceDifference = 30.00m,
            PercentagePriceChange = 7.89m,
            ShouldUpdatePurchasePrice = true
        };

        invoice.Items.Add(item);
        context.SupplierInvoices.Add(invoice);
        await context.SaveChangesAsync();

        // Verify stock has NOT changed before approval
        var preCheckProduct = await context.Products.FindAsync(product1.Id);
        preCheckProduct!.CurrentStock.Should().Be(initialStock);

        var scannerMock = new Mock<IInvoiceScannerService>();
        var envMock = new Mock<IWebHostEnvironment>();

        var invoiceService = new SupplierInvoiceService(
            context,
            userMock.Object,
            scannerMock.Object,
            envMock.Object,
            NullLogger<SupplierInvoiceService>.Instance);

        var approveDto = new ApproveSupplierInvoiceDto
        {
            Notes = "Verified and approved by store manager",
            UpdatePurchasePrices = true
        };

        var approveResult = await invoiceService.ApproveAndSyncInventoryAsync(invoice.Id, approveDto);
        approveResult.Success.Should().BeTrue();
        approveResult.Data!.Status.Should().Be(SupplierInvoiceStatus.Approved);

        // Verify stock updated atomically: 50 + 100 = 150 KG
        var updatedProduct = await context.Products.FindAsync(product1.Id);
        updatedProduct!.CurrentStock.Should().Be(150.0m);
        // Verify purchase price updated: 410.00
        updatedProduct.PurchasePrice.Should().Be(410.00m);
        // Verify selling price was NOT modified: still 490.00
        updatedProduct.SellingPrice.Should().Be(490.00m);

        // Verify Purchase record created
        var purchase = await context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.SupplierId == supplier.Id);

        purchase.Should().NotBeNull();
        purchase!.TotalAmount.Should().Be(8200.00m);
        purchase.Items.Should().HaveCount(1);
        purchase.Items.First().Quantity.Should().Be(100.0m);

        // Verify StockTransaction created
        var stockTx = await context.StockTransactions
            .FirstOrDefaultAsync(st => st.ProductId == product1.Id && st.TransactionType == TransactionType.Purchase);

        stockTx.Should().NotBeNull();
        stockTx!.PreviousStock.Should().Be(50.0m);
        stockTx.NewStock.Should().Be(150.0m);
        stockTx.Quantity.Should().Be(100.0m);

        // Verify ProductPurchasePriceHistory created
        var priceHistory = await context.ProductPurchasePriceHistories
            .FirstOrDefaultAsync(h => h.ProductId == product1.Id);

        priceHistory.Should().NotBeNull();
        priceHistory!.OldPrice.Should().Be(380.00m);
        priceHistory.NewPrice.Should().Be(410.00m);
        priceHistory.SupplierInvoiceId.Should().Be(invoice.Id);
    }
}
