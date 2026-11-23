using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CategoryService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var categories = await _context.Categories
            .Where(c => c.BusinessId == businessId)
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count(p => p.IsActive),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<CategoryDto>>.Ok(categories, "Categories retrieved successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var category = await _context.Categories
            .Where(c => c.Id == id && c.BusinessId == businessId)
            .Include(c => c.Products)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count(p => p.IsActive),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        return ApiResponse<CategoryDto>.Ok(category, "Category retrieved successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var trimmedName = dto.Name.Trim();

        var duplicateExists = await _context.Categories
            .AnyAsync(c => c.BusinessId == businessId && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictAppException($"A category named '{trimmedName}' already exists in your store.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = trimmedName,
            Description = dto.Description?.Trim() ?? string.Empty
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CategoryDto
        {
            Id = category.Id,
            BusinessId = category.BusinessId,
            Name = category.Name,
            Description = category.Description,
            ProductCount = 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return ApiResponse<CategoryDto>.Ok(resultDto, "Category created successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();
        var trimmedName = dto.Name.Trim();

        var category = await _context.Categories
            .Where(c => c.Id == id && c.BusinessId == businessId)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        var duplicateExists = await _context.Categories
            .AnyAsync(c => c.BusinessId == businessId && c.Id != id && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictAppException($"A category named '{trimmedName}' already exists in your store.");
        }

        category.Name = trimmedName;
        category.Description = dto.Description?.Trim() ?? string.Empty;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CategoryDto
        {
            Id = category.Id,
            BusinessId = category.BusinessId,
            Name = category.Name,
            Description = category.Description,
            ProductCount = category.Products.Count(p => p.IsActive),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return ApiResponse<CategoryDto>.Ok(resultDto, "Category updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var category = await _context.Categories
            .Where(c => c.Id == id && c.BusinessId == businessId)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        var hasActiveProducts = category.Products.Any(p => p.IsActive);
        if (hasActiveProducts)
        {
            throw new ValidationAppException("Category", "Cannot delete category with active products assigned. Reassign or archive the products first.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Category deleted successfully.");
    }
}
