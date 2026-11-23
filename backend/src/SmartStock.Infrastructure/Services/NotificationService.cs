using Microsoft.EntityFrameworkCore;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<IReadOnlyList<NotificationDto>>> GetNotificationsAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var query = _context.Notifications
            .Include(n => n.Product)
            .Where(n => n.BusinessId == businessId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                BusinessId = n.BusinessId,
                ProductId = n.ProductId,
                ProductName = n.Product != null ? n.Product.Name : null,
                Title = n.Title,
                Message = n.Message,
                NotificationType = n.NotificationType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<NotificationDto>>.Ok(notifications, "Notifications retrieved successfully.");
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.BusinessId == businessId, cancellationToken);

        if (notification == null)
        {
            throw new NotFoundException("Notification", id);
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, "Notification marked as read.");
    }

    public async Task<ApiResponse<bool>> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var unread = await _context.Notifications
            .Where(n => n.BusinessId == businessId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true, $"{unread.Count} notifications marked as read.");
    }

    public async Task<ApiResponse<int>> CheckAndGenerateStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var businessId = _currentUserService.GetRequiredBusinessId();

        var products = await _context.Products
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(cancellationToken);

        var existingUnreadNotifications = await _context.Notifications
            .Where(n => n.BusinessId == businessId && !n.IsRead && n.ProductId.HasValue)
            .ToDictionaryAsync(n => n.ProductId!.Value, cancellationToken);

        int newAlertsCount = 0;

        foreach (var p in products)
        {
            bool isOut = p.CurrentStock <= 0;
            bool isLow = p.CurrentStock <= p.MinimumStock;

            if (isOut || isLow)
            {
                var targetType = isOut ? NotificationType.OutOfStock : NotificationType.LowStock;

                if (existingUnreadNotifications.TryGetValue(p.Id, out var existingNote))
                {
                    // If severity changed from low to out, update it
                    if (existingNote.NotificationType != targetType)
                    {
                        existingNote.NotificationType = targetType;
                        existingNote.Title = isOut ? $"Out of Stock: {p.Name}" : $"Low Stock: {p.Name}";
                        existingNote.Message = isOut
                            ? $"{p.Name} (SKU: {p.SKU}) is completely out of stock."
                            : $"{p.Name} (SKU: {p.SKU}) current stock ({p.CurrentStock} {p.Unit}) has reached or fallen below minimum threshold ({p.MinimumStock} {p.Unit}).";
                    }
                }
                else
                {
                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = businessId,
                        ProductId = p.Id,
                        Title = isOut ? $"Out of Stock: {p.Name}" : $"Low Stock: {p.Name}",
                        Message = isOut
                            ? $"{p.Name} (SKU: {p.SKU}) is completely out of stock."
                            : $"{p.Name} (SKU: {p.SKU}) current stock ({p.CurrentStock} {p.Unit}) has reached or fallen below minimum threshold ({p.MinimumStock} {p.Unit}).",
                        NotificationType = targetType,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    newAlertsCount++;
                }
            }
            else
            {
                // Product is healthy: resolve any active unread low/out stock notifications
                if (existingUnreadNotifications.TryGetValue(p.Id, out var existingNote))
                {
                    existingNote.IsRead = true; // Auto-resolve replenished items
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<int>.Ok(newAlertsCount, $"{newAlertsCount} new stock alerts generated.");
    }
}
