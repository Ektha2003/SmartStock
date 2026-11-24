using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SupplierService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<PagedResult<SupplierDto>>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Suppliers
            .Where(s => s.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var search = pagination.Search.Trim().ToLower();
            query = query.Where(s => s.SupplierName.ToLower().Contains(search) 
                                     || (s.CompanyName != null && s.CompanyName.ToLower().Contains(search))
                                     || (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(search)) 
                                     || (s.GstNumber != null && s.GstNumber.ToLower().Contains(search))
                                     || s.Phone.Contains(search)
                                     || s.Email.ToLower().Contains(search));
        }

        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.SortDescending ? query.OrderByDescending(s => s.SupplierName) : query.OrderBy(s => s.SupplierName),
            "company" => pagination.SortDescending ? query.OrderByDescending(s => s.CompanyName) : query.OrderBy(s => s.CompanyName),
            "contact" => pagination.SortDescending ? query.OrderByDescending(s => s.ContactPerson) : query.OrderBy(s => s.ContactPerson),
            _ => pagination.SortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var rawItems = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(s => new
            {
                Supplier = s,
                InvoicesCount = s.Invoices.Count,
                PurchasesCount = s.Purchases.Count
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(x => MapToDto(x.Supplier, x.InvoicesCount, x.PurchasesCount)).ToList();

        return ApiResponse<PagedResult<SupplierDto>>.Ok(
            new PagedResult<SupplierDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Suppliers retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = await _context.Suppliers
            .Include(s => s.Invoices)
            .Include(s => s.Purchases)
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        return ApiResponse<SupplierDto>.Ok(MapToDto(supplier, supplier.Invoices.Count, supplier.Purchases.Count), "Supplier retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierDetailExtendedDto>> GetDetailExtendedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        // Get recent invoices
        var recentInvoices = await _context.SupplierInvoices
            .Where(i => i.BusinessId == businessId && i.SupplierId == id)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(10)
            .Select(i => new SupplierInvoiceHistoryDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                GrandTotal = i.GrandTotal,
                Status = i.Status,
                ItemCount = i.Items.Count,
                ApprovedAt = i.ApprovedAt
            })
            .ToListAsync(cancellationToken);

        // Get recent purchases
        var recentPurchases = await _context.Purchases
            .Where(p => p.BusinessId == businessId && p.SupplierId == id)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(10)
            .Select(p => new SupplierPurchaseHistoryDto
            {
                PurchaseId = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                PurchaseDate = p.PurchaseDate,
                TotalAmount = p.TotalAmount,
                ItemCount = p.Items.Count
            })
            .ToListAsync(cancellationToken);

        // Get supplied products
        var suppliedProducts = await GetSuppliedProductsInternalAsync(businessId, id, cancellationToken);

        var dto = new SupplierDetailExtendedDto
        {
            Id = supplier.Id,
            BusinessId = supplier.BusinessId,
            SupplierName = supplier.SupplierName,
            CompanyName = supplier.CompanyName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            GstNumber = supplier.GstNumber,
            PanNumber = supplier.PanNumber,
            BillingAddress = supplier.BillingAddress,
            Address = supplier.Address,
            PaymentTerms = supplier.PaymentTerms,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            InvoicesCount = recentInvoices.Count,
            PurchasesCount = recentPurchases.Count,
            RecentInvoices = recentInvoices,
            RecentPurchases = recentPurchases,
            SuppliedProducts = suppliedProducts
        };

        return ApiResponse<SupplierDetailExtendedDto>.Ok(dto, "Supplier extended detail retrieved successfully.");
    }

    public async Task<ApiResponse<IReadOnlyList<SupplierSuppliedProductDto>>> GetSuppliedProductsAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var products = await GetSuppliedProductsInternalAsync(businessId, supplierId, cancellationToken);
        return ApiResponse<IReadOnlyList<SupplierSuppliedProductDto>>.Ok(products, "Supplied products retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResult<SupplierInvoiceHistoryDto>>> GetSupplierInvoicesAsync(Guid supplierId, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.SupplierInvoices
            .Where(i => i.BusinessId == businessId && i.SupplierId == supplierId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(i => new SupplierInvoiceHistoryDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                GrandTotal = i.GrandTotal,
                Status = i.Status,
                ItemCount = i.Items.Count,
                ApprovedAt = i.ApprovedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<SupplierInvoiceHistoryDto>>.Ok(
            new PagedResult<SupplierInvoiceHistoryDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Supplier invoices retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResult<SupplierPurchaseHistoryDto>>> GetSupplierPurchasesAsync(Guid supplierId, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Purchases
            .Where(p => p.BusinessId == businessId && p.SupplierId == supplierId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PurchaseDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(p => new SupplierPurchaseHistoryDto
            {
                PurchaseId = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                PurchaseDate = p.PurchaseDate,
                TotalAmount = p.TotalAmount,
                ItemCount = p.Items.Count
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<SupplierPurchaseHistoryDto>>.Ok(
            new PagedResult<SupplierPurchaseHistoryDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Supplier purchases retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> CreateAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = dto.SupplierName.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            GstNumber = dto.GstNumber?.Trim().ToUpperInvariant(),
            PanNumber = dto.PanNumber?.Trim().ToUpperInvariant(),
            BillingAddress = dto.BillingAddress?.Trim(),
            Address = dto.Address?.Trim() ?? string.Empty,
            PaymentTerms = dto.PaymentTerms?.Trim(),
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierDto>.Ok(MapToDto(supplier), "Supplier created successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> UpdateAsync(Guid id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        supplier.SupplierName = dto.SupplierName.Trim();
        supplier.CompanyName = dto.CompanyName?.Trim();
        supplier.ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty;
        supplier.Phone = dto.Phone?.Trim() ?? string.Empty;
        supplier.Email = dto.Email?.Trim() ?? string.Empty;
        supplier.GstNumber = dto.GstNumber?.Trim().ToUpperInvariant();
        supplier.PanNumber = dto.PanNumber?.Trim().ToUpperInvariant();
        supplier.BillingAddress = dto.BillingAddress?.Trim();
        supplier.Address = dto.Address?.Trim() ?? string.Empty;
        supplier.PaymentTerms = dto.PaymentTerms?.Trim();
        supplier.Notes = dto.Notes?.Trim();
        supplier.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<SupplierDto>.Ok(MapToDto(supplier), "Supplier updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        // Soft delete / deactivate
        supplier.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Supplier deactivated successfully.");
    }

    private async Task<List<SupplierSuppliedProductDto>> GetSuppliedProductsInternalAsync(Guid businessId, Guid supplierId, CancellationToken cancellationToken)
    {
        // Query distinct products from purchases of this supplier or matched invoice items
        var purchaseItems = await _context.PurchaseItems
            .Include(pi => pi.Purchase)
            .Include(pi => pi.Product)
                .ThenInclude(p => p.Category)
            .Where(pi => pi.Purchase.BusinessId == businessId && pi.Purchase.SupplierId == supplierId)
            .OrderByDescending(pi => pi.Purchase.PurchaseDate)
            .ToListAsync(cancellationToken);

        var productMap = new Dictionary<Guid, SupplierSuppliedProductDto>();

        foreach (var pi in purchaseItems)
        {
            if (!productMap.ContainsKey(pi.ProductId))
            {
                productMap[pi.ProductId] = new SupplierSuppliedProductDto
                {
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    SKU = pi.Product.SKU,
                    CategoryName = pi.Product.Category?.Name ?? "Uncategorized",
                    CurrentStock = pi.Product.CurrentStock,
                    LastPurchasePrice = pi.UnitCost,
                    Unit = pi.Product.Unit,
                    LastPurchasedAt = pi.Purchase.PurchaseDate
                };
            }
        }

        return productMap.Values.ToList();
    }

    private static SupplierDto MapToDto(Supplier s, int invoicesCount = 0, int purchasesCount = 0)
    {
        return new SupplierDto
        {
            Id = s.Id,
            BusinessId = s.BusinessId,
            SupplierName = s.SupplierName,
            CompanyName = s.CompanyName,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            GstNumber = s.GstNumber,
            PanNumber = s.PanNumber,
            BillingAddress = s.BillingAddress,
            Address = s.Address,
            PaymentTerms = s.PaymentTerms,
            Notes = s.Notes,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            InvoicesCount = invoicesCount,
            PurchasesCount = purchasesCount
        };
    }
}
