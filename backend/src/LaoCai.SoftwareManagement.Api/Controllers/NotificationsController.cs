using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var pagedResult = await _notificationService.GetNotificationsAsync(
            currentUserId,
            page,
            pageSize,
            unreadOnly,
            cancellationToken);

        var unreadCount = await _notificationService.GetUnreadCountAsync(currentUserId, cancellationToken);

        return Ok(new
        {
            items = pagedResult.Items,
            totalCount = pagedResult.TotalCount,
            page = pagedResult.Page,
            pageSize = pagedResult.PageSize,
            totalPages = pagedResult.TotalPages,
            unreadCount
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var unreadCount = await _notificationService.GetUnreadCountAsync(currentUserId, cancellationToken);
        return Ok(new { count = unreadCount });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var success = await _notificationService.MarkAsReadAsync(id, currentUserId, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var count = await _notificationService.MarkAllAsReadAsync(currentUserId, cancellationToken);
        return Ok(new { markedCount = count });
    }
}
