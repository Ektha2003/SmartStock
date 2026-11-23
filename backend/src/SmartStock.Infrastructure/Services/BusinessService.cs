using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class BusinessService : IBusinessService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public BusinessService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<BusinessDto>> GetCurrentBusinessAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business == null)
        {
            throw new NotFoundException("Business not found.");
        }

        return ApiResponse<BusinessDto>.Ok(MapToDto(business), "Business retrieved successfully.");
    }

    public async Task<ApiResponse<BusinessDto>> UpdateBusinessAsync(UpdateBusinessDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business == null)
        {
            throw new NotFoundException("Business not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.BusinessName))
            business.BusinessName = dto.BusinessName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.OwnerName))
            business.OwnerName = dto.OwnerName.Trim();

        if (dto.Phone != null)
            business.Phone = dto.Phone.Trim();

        if (dto.Address != null)
            business.Address = dto.Address.Trim();

        if (!string.IsNullOrWhiteSpace(dto.BusinessType))
            business.BusinessType = dto.BusinessType.Trim();

        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<BusinessDto>.Ok(MapToDto(business), "Business updated successfully.");
    }

    private static BusinessDto MapToDto(Core.Entities.Business b)
    {
        return new BusinessDto
        {
            Id = b.Id,
            BusinessName = b.BusinessName,
            OwnerName = b.OwnerName,
            Phone = b.Phone,
            Address = b.Address,
            BusinessType = b.BusinessType,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
