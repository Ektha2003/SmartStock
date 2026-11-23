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
            .Where(s => s.BusinessId == businessId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
        {
            var search = pagination.Search.Trim().ToLower();
            query = query.Where(s => s.SupplierName.ToLower().Contains(search) 
                                     || s.ContactPerson.ToLower().Contains(search) 
                                     || s.Phone.Contains(search));
        }

        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.SortDescending ? query.OrderByDescending(s => s.SupplierName) : query.OrderBy(s => s.SupplierName),
            "contact" => pagination.SortDescending ? query.OrderByDescending(s => s.ContactPerson) : query.OrderBy(s => s.ContactPerson),
            _ => pagination.SortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<SupplierDto>>.Ok(
            new PagedResult<SupplierDto>(items, total, pagination.PageNumber, pagination.PageSize),
            "Suppliers retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, cancellationToken);

        if (supplier == null)
        {
            throw new NotFoundException("Supplier", id);
        }

        return ApiResponse<SupplierDto>.Ok(MapToDto(supplier), "Supplier retrieved successfully.");
    }

    public async Task<ApiResponse<SupplierDto>> CreateAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierName = dto.SupplierName.Trim(),
            ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = dto.Email?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
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
        supplier.ContactPerson = dto.ContactPerson?.Trim() ?? string.Empty;
        supplier.Phone = dto.Phone?.Trim() ?? string.Empty;
        supplier.Email = dto.Email?.Trim() ?? string.Empty;
        supplier.Address = dto.Address?.Trim() ?? string.Empty;
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

        // Soft delete
        supplier.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Supplier deactivated successfully.");
    }

    private static SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto
        {
            Id = s.Id,
            BusinessId = s.BusinessId,
            SupplierName = s.SupplierName,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
