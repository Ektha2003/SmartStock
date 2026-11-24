using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.AI;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class SupplierInvoiceService : ISupplierInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInvoiceScannerService _invoiceScannerService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SupplierInvoiceService> _logger;

    public SupplierInvoiceService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IInvoiceScannerService invoiceScannerService,
        IWebHostEnvironment environment,
        ILogger<SupplierInvoiceService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _invoiceScannerService = invoiceScannerService;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> UploadAndProcessInvoiceAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ValidationAppException("File", "Invoice file must not be empty.");
        }

        // 1. Read bytes & compute SHA256 document hash
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        string documentHash;
        using (var sha = SHA256.Create())
        {
            var hashBytes = sha.ComputeHash(fileBytes);
            documentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // 2. Run OCR/AI extraction first. It throws a user-readable AppException when the
        //    invoice cannot be read, so nothing is saved for a failed scan.
        memoryStream.Position = 0;
        var extractionResult = await _invoiceScannerService.ExtractInvoiceAsync(memoryStream, fileName, contentType, cancellationToken);
        var extractedData = extractionResult.Data
            ?? throw new AppException("The AI service returned no invoice data. Please try again.", 502);

        // 3. Save physical file securely in uploads/invoices/{businessId}
        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "invoices", businessId.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(uploadsFolder, safeFileName);
        await File.WriteAllBytesAsync(fullPath, fileBytes, cancellationToken);
        var relativeFilePath = $"/uploads/invoices/{businessId}/{safeFileName}";

        // 4. Resolve or verify supplier
        Supplier? matchedSupplier = null;
        if (supplierId.HasValue)
        {
            matchedSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId.Value && s.BusinessId == businessId, cancellationToken);
        }

        if (matchedSupplier == null && !string.IsNullOrWhiteSpace(extractedData.SupplierGst))
        {
            var gst = extractedData.SupplierGst.Trim().ToUpperInvariant();
            matchedSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && s.GstNumber != null && s.GstNumber.ToUpper() == gst, cancellationToken);
        }

        if (matchedSupplier == null && !string.IsNullOrWhiteSpace(extractedData.SupplierName))
        {
            var name = extractedData.SupplierName.Trim().ToLower();
            matchedSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.BusinessId == businessId && (s.SupplierName.ToLower() == name || (s.CompanyName != null && s.CompanyName.ToLower() == name)), cancellationToken);
        }

        // 5. Check duplicate invoice (same hash OR same invoice number + supplier)
        var isDuplicate = false;
        string? duplicateReason = null;

        var existingWithHash = await _context.SupplierInvoices
            .FirstOrDefaultAsync(i => i.BusinessId == businessId && i.DocumentHash == documentHash, cancellationToken);

        if (existingWithHash != null)
        {
            isDuplicate = true;
            duplicateReason = $"Duplicate file detected: Exactly matches previously uploaded invoice #{existingWithHash.InvoiceNumber}.";
        }
        else if (matchedSupplier != null && !string.IsNullOrWhiteSpace(extractedData.InvoiceNumber))
        {
            var existingWithNumber = await _context.SupplierInvoices
                .FirstOrDefaultAsync(i => i.BusinessId == businessId && i.SupplierId == matchedSupplier.Id && i.InvoiceNumber.ToLower() == extractedData.InvoiceNumber.Trim().ToLower(), cancellationToken);

            if (existingWithNumber != null)
            {
                isDuplicate = true;
                duplicateReason = $"Duplicate invoice number '{extractedData.InvoiceNumber}' already exists for supplier '{matchedSupplier.SupplierName}'.";
            }
        }

        // 6. Fetch business catalog products for matching
        var catalogProducts = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        // 7. Create SupplierInvoice Entity
        var invoiceDate = extractedData.InvoiceDate.HasValue 
            ? DateTime.SpecifyKind(extractedData.InvoiceDate.Value, DateTimeKind.Utc) 
            : DateTime.UtcNow;

        var invoice = new SupplierInvoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierId = matchedSupplier?.Id,
            Supplier = matchedSupplier,
            InvoiceNumber = string.IsNullOrWhiteSpace(extractedData.InvoiceNumber) ? $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}" : extractedData.InvoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            DueDate = extractedData.DueDate.HasValue ? DateTime.SpecifyKind(extractedData.DueDate.Value, DateTimeKind.Utc) : null,
            PaymentTerms = extractedData.PaymentTerms ?? matchedSupplier?.PaymentTerms,
            SupplierNameRaw = extractedData.SupplierName,
            SupplierGst = extractedData.SupplierGst ?? matchedSupplier?.GstNumber,
            BillingAddress = extractedData.BillingAddress ?? matchedSupplier?.BillingAddress,
            DeliveryAddress = extractedData.DeliveryAddress,
            PurchaseOrderNumber = extractedData.PurchaseOrderNumber,
            SubTotal = extractedData.SubTotal > 0 ? extractedData.SubTotal : extractedData.TotalAmount,
            Discount = extractedData.Discount,
            TaxableAmount = extractedData.TaxableAmount,
            Cgst = extractedData.Cgst,
            Sgst = extractedData.Sgst,
            Igst = extractedData.Igst,
            OtherCharges = extractedData.OtherCharges,
            GrandTotal = extractedData.TotalAmount > 0 ? extractedData.TotalAmount : extractedData.SubTotal,
            OriginalFileName = fileName,
            FilePath = relativeFilePath,
            ContentType = contentType,
            DocumentHash = documentHash,
            FileSizeBytes = fileBytes.Length,
            Status = isDuplicate ? SupplierInvoiceStatus.Duplicate : SupplierInvoiceStatus.ReadyForReview,
            IsDuplicate = isDuplicate,
            DuplicateReason = duplicateReason,
            ExtractedRawJson = extractedData.RawExtractedJson,
            Notes = extractedData.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // 8. Process Line Items and Product Matching
        var invoiceItems = new List<SupplierInvoiceItem>();

        foreach (var extractedItem in extractedData.Items)
        {
            var matchEval = ProductMatchingEngine.EvaluateMatch(
                extractedItem.ProductName,
                extractedItem.Description,
                extractedItem.SupplierSku,
                extractedItem.HsnCode,
                extractedItem.Quantity,
                extractedItem.Unit,
                extractedItem.UnitPrice,
                catalogProducts);

            var lineItem = new SupplierInvoiceItem
            {
                Id = Guid.NewGuid(),
                SupplierInvoiceId = invoice.Id,
                RawProductName = extractedItem.ProductName,
                RawDescription = extractedItem.Description,
                RawSupplierSku = extractedItem.SupplierSku,
                RawHsnCode = extractedItem.HsnCode,
                InvoiceQuantity = extractedItem.Quantity,
                InvoiceUnit = string.IsNullOrWhiteSpace(extractedItem.Unit) ? "PCS" : extractedItem.Unit.Trim().ToUpperInvariant(),
                InvoiceUnitPrice = extractedItem.UnitPrice,
                Discount = extractedItem.Discount,
                TaxPercentage = extractedItem.TaxPercentage,
                TaxAmount = extractedItem.TaxAmount,
                LineTotal = extractedItem.LineTotal > 0 ? extractedItem.LineTotal : Math.Round(extractedItem.Quantity * extractedItem.UnitPrice, 2),
                MatchedProductId = matchEval.MatchedProduct?.Id,
                MatchedProduct = matchEval.MatchedProduct,
                MatchStatus = matchEval.Status,
                MatchConfidence = matchEval.Confidence,
                MatchExplanation = matchEval.Explanation,
                ConversionFactor = matchEval.ConversionFactor,
                PackSize = matchEval.PackSize,
                CalculatedStockQuantity = matchEval.CalculatedStockQuantity,
                TargetUnit = matchEval.MatchedProduct?.Unit ?? matchEval.TargetUnit,
                NewPurchasePrice = extractedItem.UnitPrice,
                ShouldUpdatePurchasePrice = true
            };

            if (matchEval.MatchedProduct != null)
            {
                lineItem.PreviousPurchasePrice = matchEval.MatchedProduct.PurchasePrice;
                lineItem.PriceDifference = lineItem.NewPurchasePrice - matchEval.MatchedProduct.PurchasePrice;
                lineItem.PercentagePriceChange = matchEval.MatchedProduct.PurchasePrice > 0
                    ? Math.Round((lineItem.PriceDifference / matchEval.MatchedProduct.PurchasePrice) * 100, 2)
                    : 0;
            }

            invoiceItems.Add(lineItem);
        }

        invoice.Items = invoiceItems;

        // 9. Processing Audit Log
        var processingLog = new AIInvoiceProcessingLog
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierInvoiceId = invoice.Id,
            Stage = "OCR_AND_PRODUCT_MATCHING",
            ConfidenceScore = invoiceItems.Count > 0 ? invoiceItems.Average(i => i.MatchConfidence) : 0,
            ExtractedDataJson = extractedData.RawExtractedJson,
            ExecutionSummary = $"Extracted {invoiceItems.Count} items. {invoiceItems.Count(i => i.MatchedProductId != null)} matched with inventory.",
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierInvoices.Add(invoice);
        _context.AIInvoiceProcessingLogs.Add(processingLog);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierInvoiceDto>.Ok(MapToDto(invoice), isDuplicate ? "Invoice uploaded - Duplicate detected." : "Invoice uploaded and processed successfully.");
    }

    public async Task<ApiResponse<PagedResult<SupplierInvoiceDto>>> GetInvoicesAsync(
        SupplierInvoiceFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .Where(i => i.BusinessId == businessId);

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == filter.SupplierId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDate >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDate <= toUtc);
        }

        if (filter.HasPriceIncreases == true)
        {
            query = query.Where(i => i.Items.Any(item => item.PriceDifference > 0));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(search)
                                     || (i.SupplierNameRaw != null && i.SupplierNameRaw.ToLower().Contains(search))
                                     || (i.Supplier != null && i.Supplier.SupplierName.ToLower().Contains(search)));
        }

        query = filter.SortBy?.ToLower() switch
        {
            "invoicenumber" => filter.SortDescending ? query.OrderByDescending(i => i.InvoiceNumber) : query.OrderBy(i => i.InvoiceNumber),
            "amount" or "grandtotal" => filter.SortDescending ? query.OrderByDescending(i => i.GrandTotal) : query.OrderBy(i => i.GrandTotal),
            "status" => filter.SortDescending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            _ => filter.SortDescending ? query.OrderByDescending(i => i.InvoiceDate) : query.OrderByDescending(i => i.InvoiceDate)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return ApiResponse<PagedResult<SupplierInvoiceDto>>.Ok(
            new PagedResult<SupplierInvoiceDto>(dtos, total, filter.PageNumber, filter.PageSize),
            "Supplier invoices retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> GetInvoiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", id);
        }

        return ApiResponse<SupplierInvoiceDto>.Ok(MapToDto(invoice), "Invoice details retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> RematchProductsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", id);
        }

        if (invoice.Status == SupplierInvoiceStatus.Approved)
        {
            throw new ValidationAppException("Status", "Cannot rematch products on an already approved invoice.");
        }

        var catalogProducts = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var item in invoice.Items)
        {
            // Do not override user-confirmed or new-product items unless unlinked
            if (item.MatchStatus == ProductMatchStatus.UserConfirmed || item.MatchStatus == ProductMatchStatus.NewProductCreated)
            {
                continue;
            }

            var eval = ProductMatchingEngine.EvaluateMatch(
                item.RawProductName,
                item.RawDescription,
                item.RawSupplierSku,
                item.RawHsnCode,
                item.InvoiceQuantity,
                item.InvoiceUnit,
                item.InvoiceUnitPrice,
                catalogProducts);

            item.MatchedProductId = eval.MatchedProduct?.Id;
            item.MatchedProduct = eval.MatchedProduct;
            item.MatchStatus = eval.Status;
            item.MatchConfidence = eval.Confidence;
            item.MatchExplanation = eval.Explanation;
            item.ConversionFactor = eval.ConversionFactor;
            item.PackSize = eval.PackSize;
            item.CalculatedStockQuantity = eval.CalculatedStockQuantity;
            item.TargetUnit = eval.MatchedProduct?.Unit ?? eval.TargetUnit;

            if (eval.MatchedProduct != null)
            {
                item.PreviousPurchasePrice = eval.MatchedProduct.PurchasePrice;
                item.PriceDifference = item.NewPurchasePrice - eval.MatchedProduct.PurchasePrice;
                item.PercentagePriceChange = eval.MatchedProduct.PurchasePrice > 0
                    ? Math.Round((item.PriceDifference / eval.MatchedProduct.PurchasePrice) * 100, 2)
                    : 0;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierInvoiceDto>.Ok(MapToDto(invoice), "Products rematched successfully against inventory.");
    }

    public async Task<ApiResponse<SupplierInvoiceItemDto>> UpdateInvoiceItemAsync(
        Guid invoiceId,
        Guid itemId,
        UpdateSupplierInvoiceItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", invoiceId);
        }

        if (invoice.Status == SupplierInvoiceStatus.Approved)
        {
            throw new ValidationAppException("Status", "Cannot modify items of an approved invoice.");
        }

        var item = invoice.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            throw new NotFoundException("SupplierInvoiceItem", itemId);
        }

        if (dto.MatchedProductId.HasValue)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == dto.MatchedProductId.Value && p.BusinessId == businessId, cancellationToken);

            if (product == null)
            {
                throw new ValidationAppException("MatchedProductId", "Selected product does not exist in your store.");
            }

            item.MatchedProductId = product.Id;
            item.MatchedProduct = product;
            item.MatchStatus = ProductMatchStatus.UserConfirmed;
            item.MatchConfidence = 1.0m;
            item.MatchExplanation = $"Manually confirmed match with '{product.Name}'.";
            item.PreviousPurchasePrice = product.PurchasePrice;
            item.TargetUnit = product.Unit;
            item.PriceDifference = item.NewPurchasePrice - product.PurchasePrice;
            item.PercentagePriceChange = product.PurchasePrice > 0
                ? Math.Round((item.PriceDifference / product.PurchasePrice) * 100, 2)
                : 0;
        }

        if (dto.ConversionFactor.HasValue && dto.ConversionFactor.Value > 0)
        {
            item.ConversionFactor = dto.ConversionFactor.Value;
            item.CalculatedStockQuantity = item.InvoiceQuantity * item.ConversionFactor;
        }

        if (dto.PackSize.HasValue)
        {
            item.PackSize = dto.PackSize.Value;
        }

        if (dto.CalculatedStockQuantity.HasValue && dto.CalculatedStockQuantity.Value >= 0)
        {
            item.CalculatedStockQuantity = dto.CalculatedStockQuantity.Value;
        }

        if (dto.TargetUnit.HasValue)
        {
            item.TargetUnit = dto.TargetUnit.Value;
        }

        if (dto.NewPurchasePrice.HasValue && dto.NewPurchasePrice.Value >= 0)
        {
            item.NewPurchasePrice = dto.NewPurchasePrice.Value;
            if (item.PreviousPurchasePrice.HasValue)
            {
                item.PriceDifference = item.NewPurchasePrice - item.PreviousPurchasePrice.Value;
                item.PercentagePriceChange = item.PreviousPurchasePrice.Value > 0
                    ? Math.Round((item.PriceDifference / item.PreviousPurchasePrice.Value) * 100, 2)
                    : 0;
            }
        }

        if (dto.ShouldUpdatePurchasePrice.HasValue)
        {
            item.ShouldUpdatePurchasePrice = dto.ShouldUpdatePurchasePrice.Value;
        }

        if (dto.IsIgnored.HasValue)
        {
            item.IsIgnored = dto.IsIgnored.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierInvoiceItemDto>.Ok(MapToItemDto(item), "Invoice line item updated successfully.");
    }

    public async Task<ApiResponse<SupplierInvoiceItemDto>> ConfirmProductMatchAsync(
        Guid invoiceId,
        ConfirmProductMatchDto dto,
        CancellationToken cancellationToken = default)
    {
        return await UpdateInvoiceItemAsync(invoiceId, dto.InvoiceItemId, new UpdateSupplierInvoiceItemDto
        {
            MatchedProductId = dto.ProductId,
            ConversionFactor = dto.ConversionFactor,
            PackSize = dto.PackSize,
            ShouldUpdatePurchasePrice = dto.ShouldUpdatePurchasePrice
        }, cancellationToken);
    }

    public async Task<ApiResponse<SupplierInvoiceItemDto>> CreateProductFromInvoiceItemAsync(
        Guid invoiceId,
        CreateProductFromInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", invoiceId);
        }

        var item = invoice.Items.FirstOrDefault(i => i.Id == dto.InvoiceItemId);
        if (item == null)
        {
            throw new NotFoundException("SupplierInvoiceItem", dto.InvoiceItemId);
        }

        // Verify category
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.BusinessId == businessId, cancellationToken);

        if (category == null)
        {
            throw new ValidationAppException("CategoryId", "Selected category does not exist.");
        }

        // Generate or validate SKU
        var sku = string.IsNullOrWhiteSpace(dto.SKU)
            ? $"SKU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}"
            : dto.SKU.Trim().ToUpperInvariant();

        var existingWithSku = await _context.Products
            .AnyAsync(p => p.BusinessId == businessId && p.SKU == sku, cancellationToken);

        if (existingWithSku)
        {
            sku = $"{sku}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
        }

        var effectiveSellingPrice = dto.SellingPrice > 0 
            ? dto.SellingPrice 
            : Math.Round(dto.PurchasePrice * 1.25m, 2);

        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = dto.CategoryId,
            Name = dto.Name.Trim(),
            SKU = sku,
            Description = dto.Description?.Trim() ?? string.Empty,
            PurchasePrice = dto.PurchasePrice,
            SellingPrice = effectiveSellingPrice,
            CurrentStock = 0, // Stock will be credited upon invoice approval
            MinimumStock = dto.MinimumStock > 0 ? dto.MinimumStock : 5,
            Unit = dto.Unit,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(newProduct);

        // Update item
        item.MatchedProductId = newProduct.Id;
        item.MatchedProduct = newProduct;
        item.MatchStatus = ProductMatchStatus.NewProductCreated;
        item.MatchConfidence = 1.0m;
        item.MatchExplanation = $"Created new inventory product '{newProduct.Name}' ({newProduct.SKU}).";
        item.ConversionFactor = dto.ConversionFactor > 0 ? dto.ConversionFactor : 1.0m;
        item.PackSize = dto.PackSize;
        item.CalculatedStockQuantity = item.InvoiceQuantity * item.ConversionFactor;
        item.TargetUnit = dto.Unit;
        item.NewPurchasePrice = dto.PurchasePrice;
        item.PreviousPurchasePrice = 0;
        item.PriceDifference = 0;
        item.PercentagePriceChange = 0;
        item.ShouldUpdatePurchasePrice = true;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierInvoiceItemDto>.Ok(MapToItemDto(item), "New product created and linked to invoice line item.");
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> ApproveAndSyncInventoryAsync(
        Guid invoiceId,
        ApproveSupplierInvoiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var currentUserId = _currentUserService.UserId;
        var currentUserEmail = _currentUserService.Email ?? "Administrator";

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", invoiceId);
        }

        if (invoice.Status == SupplierInvoiceStatus.Approved)
        {
            throw new ValidationAppException("Status", "This invoice has already been approved and synced with inventory.");
        }

        var activeItems = invoice.Items.Where(it => !it.IsIgnored).ToList();
        if (activeItems.Count == 0)
        {
            throw new ValidationAppException("Items", "Cannot approve an invoice with zero active items.");
        }

        // Check if any non-ignored item lacks a matched product
        var unlinkedItems = activeItems.Where(it => it.MatchedProductId == null).ToList();
        if (unlinkedItems.Count > 0)
        {
            throw new ValidationAppException("Items", $"Cannot approve invoice: {unlinkedItems.Count} item(s) are not matched with inventory. Please match them or mark them as ignored.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
            try
            {
                // Create Purchase record
                var dateStr = invoice.InvoiceDate.ToString("yyyyMMdd");
                var purchaseCountToday = await _context.Purchases
                    .CountAsync(p => p.BusinessId == businessId && p.InvoiceNumber.StartsWith($"PUR-{dateStr}"), cancellationToken);
                var purchaseInvoiceNumber = $"PUR-{dateStr}-{(purchaseCountToday + 1):D4}";

                var purchase = new Purchase
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    SupplierId = invoice.SupplierId,
                    InvoiceNumber = purchaseInvoiceNumber,
                    PurchaseDate = invoice.InvoiceDate,
                    Notes = $"Synced from Supplier Invoice #{invoice.InvoiceNumber}. {dto.Notes}".Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                decimal calculatedPurchaseTotal = 0;
                var purchaseItems = new List<PurchaseItem>();
                var stockTransactions = new List<StockTransaction>();
                var priceHistories = new List<ProductPurchasePriceHistory>();

                foreach (var item in activeItems)
                {
                    var product = item.MatchedProduct 
                        ?? await _context.Products.FirstAsync(p => p.Id == item.MatchedProductId!.Value, cancellationToken);

                    var prevStock = product.CurrentStock;
                    var addedStock = item.CalculatedStockQuantity > 0 ? item.CalculatedStockQuantity : (item.InvoiceQuantity * item.ConversionFactor);
                    var newStock = prevStock + addedStock;

                    // Update stock
                    product.CurrentStock = newStock;

                    // Update purchase price if configured
                    if (dto.UpdatePurchasePrices && item.ShouldUpdatePurchasePrice && item.NewPurchasePrice > 0)
                    {
                        var oldPrice = product.PurchasePrice;
                        if (oldPrice != item.NewPurchasePrice)
                        {
                            priceHistories.Add(new ProductPurchasePriceHistory
                            {
                                Id = Guid.NewGuid(),
                                BusinessId = businessId,
                                ProductId = product.Id,
                                SupplierId = invoice.SupplierId,
                                SupplierInvoiceId = invoice.Id,
                                PurchaseId = purchase.Id,
                                OldPrice = oldPrice,
                                NewPrice = item.NewPurchasePrice,
                                EffectiveDate = DateTime.UtcNow,
                                Notes = $"Price updated via Supplier Invoice #{invoice.InvoiceNumber}"
                            });

                            product.PurchasePrice = item.NewPurchasePrice;
                        }
                    }

                    product.UpdatedAt = DateTime.UtcNow;

                    var lineItemTotal = item.LineTotal > 0 ? item.LineTotal : Math.Round(item.InvoiceQuantity * item.InvoiceUnitPrice, 2);
                    calculatedPurchaseTotal += lineItemTotal;

                    purchaseItems.Add(new PurchaseItem
                    {
                        Id = Guid.NewGuid(),
                        PurchaseId = purchase.Id,
                        ProductId = product.Id,
                        Quantity = addedStock,
                        UnitCost = item.NewPurchasePrice,
                        SubTotal = lineItemTotal
                    });

                    stockTransactions.Add(new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = product.Id,
                        TransactionType = TransactionType.Purchase,
                        Quantity = addedStock,
                        PreviousStock = prevStock,
                        NewStock = newStock,
                        ReferenceId = purchase.Id,
                        Notes = $"Supplier Invoice #{invoice.InvoiceNumber} ({addedStock} {product.Unit})",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                purchase.TotalAmount = calculatedPurchaseTotal;
                purchase.Items = purchaseItems;

                _context.Purchases.Add(purchase);
                _context.StockTransactions.AddRange(stockTransactions);
                if (priceHistories.Count > 0)
                {
                    _context.ProductPurchasePriceHistories.AddRange(priceHistories);
                }

                // Update invoice status
                invoice.Status = SupplierInvoiceStatus.Approved;
                invoice.ApprovedAt = DateTime.UtcNow;
                invoice.ApprovedByUserId = currentUserId;
                invoice.ApprovedByUserName = currentUserEmail;
                invoice.ConfirmedPurchaseId = purchase.Id;
                if (!string.IsNullOrWhiteSpace(dto.Notes))
                {
                    invoice.Notes = $"{invoice.Notes} | Approval Notes: {dto.Notes}".Trim();
                }

                // Audit log
                _context.AIInvoiceProcessingLogs.Add(new AIInvoiceProcessingLog
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    SupplierInvoiceId = invoice.Id,
                    Stage = "APPROVED_AND_SYNCED",
                    ConfidenceScore = 1.0m,
                    ExecutionSummary = $"Approved by {currentUserEmail}. Synced {activeItems.Count} items to stock. Created purchase {purchaseInvoiceNumber}.",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ApiResponse<SupplierInvoiceDto>.Ok(MapToDto(invoice), $"Invoice #{invoice.InvoiceNumber} approved and inventory stock successfully updated!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to approve and sync supplier invoice {InvoiceId}", invoiceId);
                throw;
            }
        });
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> RejectInvoiceAsync(
        Guid invoiceId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        if (invoice == null)
        {
            throw new NotFoundException("SupplierInvoice", invoiceId);
        }

        if (invoice.Status == SupplierInvoiceStatus.Approved)
        {
            throw new ValidationAppException("Status", "Cannot reject an invoice that has already been approved and credited to stock.");
        }

        invoice.Status = SupplierInvoiceStatus.Rejected;
        invoice.Notes = $"{invoice.Notes} | Rejected: {reason}".Trim();

        _context.AIInvoiceProcessingLogs.Add(new AIInvoiceProcessingLog
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierInvoiceId = invoice.Id,
            Stage = "REJECTED",
            ConfidenceScore = 0.0m,
            ExecutionSummary = $"Invoice rejected. Reason: {reason}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierInvoiceDto>.Ok(MapToDto(invoice), "Invoice rejected successfully.");
    }

    public async Task<ApiResponse<SupplierInvoiceDashboardMetricsDto>> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var invoices = await _context.SupplierInvoices
            .Include(i => i.Items)
            .Where(i => i.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var metrics = new SupplierInvoiceDashboardMetricsDto
        {
            TotalInvoices = invoices.Count,
            ReadyForReviewCount = invoices.Count(i => i.Status == SupplierInvoiceStatus.ReadyForReview),
            ApprovedCount = invoices.Count(i => i.Status == SupplierInvoiceStatus.Approved),
            RejectedCount = invoices.Count(i => i.Status == SupplierInvoiceStatus.Rejected),
            DuplicateCount = invoices.Count(i => i.Status == SupplierInvoiceStatus.Duplicate || i.IsDuplicate),
            TotalAmountThisMonth = invoices.Where(i => i.InvoiceDate >= firstDayOfMonth && i.Status == SupplierInvoiceStatus.Approved).Sum(i => i.GrandTotal),
            TotalPriceHikesDetected = invoices.SelectMany(i => i.Items).Count(it => it.PriceDifference > 0)
        };

        return ApiResponse<SupplierInvoiceDashboardMetricsDto>.Ok(metrics, "Supplier invoice dashboard metrics retrieved.");
    }

    public async Task<ApiResponse<InvoiceComparisonDto>> CompareInvoiceWithPreviousAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var currentInvoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.BusinessId == businessId, cancellationToken);

        if (currentInvoice == null)
        {
            throw new NotFoundException("SupplierInvoice", invoiceId);
        }

        // Find previous invoice from the same supplier or previous invoice overall
        var previousInvoice = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .Where(i => i.BusinessId == businessId 
                        && i.Id != currentInvoice.Id 
                        && i.InvoiceDate < currentInvoice.InvoiceDate
                        && (currentInvoice.SupplierId == null || i.SupplierId == currentInvoice.SupplierId))
            .OrderByDescending(i => i.InvoiceDate)
            .FirstOrDefaultAsync(cancellationToken);

        var itemComparisons = new List<InvoiceItemComparisonDto>();

        foreach (var curItem in currentInvoice.Items.Where(i => !i.IsIgnored))
        {
            var prevItem = previousInvoice?.Items
                .FirstOrDefault(pi => (curItem.MatchedProductId != null && pi.MatchedProductId == curItem.MatchedProductId)
                                      || string.Equals(pi.RawProductName, curItem.RawProductName, StringComparison.OrdinalIgnoreCase));

            var prevPrice = prevItem?.NewPurchasePrice ?? curItem.PreviousPurchasePrice ?? 0;
            var prevQty = prevItem?.InvoiceQuantity ?? 0;

            itemComparisons.Add(new InvoiceItemComparisonDto
            {
                ProductName = curItem.MatchedProduct?.Name ?? curItem.RawProductName,
                CurrentPrice = curItem.NewPurchasePrice,
                PreviousPrice = prevPrice,
                CurrentQuantity = curItem.InvoiceQuantity,
                PreviousQuantity = prevQty,
                Unit = curItem.InvoiceUnit
            });
        }

        var totalPriceVariance = currentInvoice.GrandTotal - (previousInvoice?.GrandTotal ?? 0);
        var percentVariance = (previousInvoice != null && previousInvoice.GrandTotal > 0)
            ? Math.Round((totalPriceVariance / previousInvoice.GrandTotal) * 100, 2)
            : 0;

        var comparison = new InvoiceComparisonDto
        {
            CurrentInvoice = MapToDto(currentInvoice),
            PreviousInvoice = previousInvoice != null ? MapToDto(previousInvoice) : null,
            ItemComparisons = itemComparisons,
            TotalPriceVariance = totalPriceVariance,
            PercentageVariance = percentVariance
        };

        return ApiResponse<InvoiceComparisonDto>.Ok(comparison, "Invoice comparison generated successfully.");
    }

    public async Task<ApiResponse<AIInvoiceAssistantResponseDto>> QueryAssistantAsync(
        AIInvoiceAssistantQueryDto dto,
        CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var query = dto.Query.Trim().ToLower();

        var invoices = await _context.SupplierInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
                .ThenInclude(it => it.MatchedProduct)
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var priceHistories = await _context.ProductPurchasePriceHistories
            .Include(h => h.Product)
            .Include(h => h.Supplier)
            .Where(h => h.BusinessId == businessId)
            .OrderByDescending(h => h.EffectiveDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var response = new AIInvoiceAssistantResponseDto();

        if (query.Contains("price increase") || query.Contains("price hike") || query.Contains("higher price") || query.Contains("variance"))
        {
            var hikes = invoices.SelectMany(i => i.Items.Where(it => it.PriceDifference > 0).Select(it => new { Invoice = i, Item = it })).ToList();
            if (hikes.Count == 0)
            {
                response.Answer = "Good news! There are no purchase price increases detected across your uploaded invoices.";
            }
            else
            {
                var summaryList = hikes.Take(5).Select(h => 
                    $"• **{h.Item.RawProductName}**: ₹{h.Item.PreviousPurchasePrice} ➔ ₹{h.Item.NewPurchasePrice} (+{h.Item.PercentagePriceChange}%) on Invoice #{h.Invoice.InvoiceNumber}");

                response.Answer = $"Identified **{hikes.Count}** item price increases across your supplier invoices:\n\n" + string.Join("\n", summaryList);
                response.SuggestedActions = new List<string>
                {
                    "Review high confidence price changes before approval",
                    "Compare side-by-side with previous supplier invoices",
                    "Adjust consumer selling prices to maintain margins"
                };
            }
        }
        else if (query.Contains("duplicate") || query.Contains("check duplicate"))
        {
            var duplicates = invoices.Where(i => i.IsDuplicate || i.Status == SupplierInvoiceStatus.Duplicate).ToList();
            if (duplicates.Count == 0)
            {
                response.Answer = "No duplicate invoices detected in your store repository. Every invoice hash and number is unique.";
            }
            else
            {
                response.Answer = $"Detected **{duplicates.Count} duplicate invoices**.\n\n" +
                    string.Join("\n", duplicates.Select(d => $"• **Invoice #{d.InvoiceNumber}** ({d.OriginalFileName}): {d.DuplicateReason}"));
                response.SuggestedActions = new List<string> { "Reject duplicate invoices", "Delete corrupted scans" };
            }
        }
        else if (query.Contains("pending") || query.Contains("review") || query.Contains("approval"))
        {
            var pending = invoices.Where(i => i.Status == SupplierInvoiceStatus.ReadyForReview).ToList();
            response.Answer = $"You have **{pending.Count} invoice(s)** pending review and approval totaling **₹{pending.Sum(p => p.GrandTotal):N2}**.";
            response.SuggestedActions = new List<string> { "Open Invoice Review screen", "Batch rematch products" };
        }
        else
        {
            response.Answer = $"SmartStock Supplier AI Assistant has indexed **{invoices.Count} invoices** and **{priceHistories.Count} price historical records**. You can ask about price hikes, supplier comparisons, duplicate invoices, or pending stock approvals.";
            response.SuggestedActions = new List<string>
            {
                "Which items had a price increase?",
                "Are there any duplicate invoices?",
                "How many invoices are pending review?"
            };
        }

        response.RelatedInvoices = invoices.Take(5).Select(MapToDto).ToList();
        return ApiResponse<AIInvoiceAssistantResponseDto>.Ok(response, "AI response generated.");
    }

    private static SupplierInvoiceDto MapToDto(SupplierInvoice i)
    {
        return new SupplierInvoiceDto
        {
            Id = i.Id,
            BusinessId = i.BusinessId,
            SupplierId = i.SupplierId,
            SupplierName = i.Supplier?.SupplierName ?? i.SupplierNameRaw,
            SupplierCompanyName = i.Supplier?.CompanyName,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            PaymentTerms = i.PaymentTerms,
            SupplierNameRaw = i.SupplierNameRaw,
            SupplierGst = i.SupplierGst,
            BillingAddress = i.BillingAddress,
            DeliveryAddress = i.DeliveryAddress,
            PurchaseOrderNumber = i.PurchaseOrderNumber,
            SubTotal = i.SubTotal,
            Discount = i.Discount,
            TaxableAmount = i.TaxableAmount,
            Cgst = i.Cgst,
            Sgst = i.Sgst,
            Igst = i.Igst,
            OtherCharges = i.OtherCharges,
            GrandTotal = i.GrandTotal,
            OriginalFileName = i.OriginalFileName,
            FilePath = i.FilePath,
            ContentType = i.ContentType,
            DocumentHash = i.DocumentHash,
            FileSizeBytes = i.FileSizeBytes,
            Status = i.Status,
            IsDuplicate = i.IsDuplicate,
            DuplicateReason = i.DuplicateReason,
            Notes = i.Notes,
            ApprovedAt = i.ApprovedAt,
            ApprovedByUserId = i.ApprovedByUserId,
            ApprovedByUserName = i.ApprovedByUserName,
            ConfirmedPurchaseId = i.ConfirmedPurchaseId,
            CreatedAt = i.CreatedAt,
            Items = i.Items.Select(MapToItemDto).ToList()
        };
    }

    private static SupplierInvoiceItemDto MapToItemDto(SupplierInvoiceItem it)
    {
        return new SupplierInvoiceItemDto
        {
            Id = it.Id,
            SupplierInvoiceId = it.SupplierInvoiceId,
            RawProductName = it.RawProductName,
            RawDescription = it.RawDescription,
            RawSupplierSku = it.RawSupplierSku,
            RawHsnCode = it.RawHsnCode,
            InvoiceQuantity = it.InvoiceQuantity,
            InvoiceUnit = it.InvoiceUnit,
            InvoiceUnitPrice = it.InvoiceUnitPrice,
            Discount = it.Discount,
            TaxPercentage = it.TaxPercentage,
            TaxAmount = it.TaxAmount,
            LineTotal = it.LineTotal,
            MatchedProductId = it.MatchedProductId,
            MatchedProductName = it.MatchedProduct?.Name,
            MatchedProductSku = it.MatchedProduct?.SKU,
            MatchedProductCurrentStock = it.MatchedProduct?.CurrentStock,
            MatchedProductUnit = it.MatchedProduct?.Unit,
            MatchStatus = it.MatchStatus,
            MatchConfidence = it.MatchConfidence,
            MatchExplanation = it.MatchExplanation,
            ConversionFactor = it.ConversionFactor,
            PackSize = it.PackSize,
            CalculatedStockQuantity = it.CalculatedStockQuantity,
            TargetUnit = it.TargetUnit,
            PreviousPurchasePrice = it.PreviousPurchasePrice,
            NewPurchasePrice = it.NewPurchasePrice,
            PriceDifference = it.PriceDifference,
            PercentagePriceChange = it.PercentagePriceChange,
            ShouldUpdatePurchasePrice = it.ShouldUpdatePurchasePrice,
            IsIgnored = it.IsIgnored
        };
    }
}
