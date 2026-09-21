using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    string Type,
    string Title,
    string Message,
    string? TargetRoute,
    DateTime? ReadAt,
    DateTime CreatedAt);

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> CreateNotificationAsync(
        Guid recipientUserId,
        string type,
        string title,
        string message,
        string? targetRoute = null,
        string? deduplicationKey = null,
        CancellationToken cancellationToken = default);
}
