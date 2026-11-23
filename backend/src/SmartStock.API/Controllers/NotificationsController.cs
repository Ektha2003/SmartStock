using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Interfaces;

namespace SmartStock.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Retrieve low stock and operational alert notifications.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetNotificationsAsync(unreadOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Mark a single notification alert as read.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Mark all alerts as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger an immediate stock evaluation scan and generate new notifications for low/out-of-stock items.
    /// </summary>
    [HttpPost("check-stock")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckStock(CancellationToken cancellationToken)
    {
        var result = await _notificationService.CheckAndGenerateStockAlertsAsync(cancellationToken);
        return Ok(result);
    }
}
