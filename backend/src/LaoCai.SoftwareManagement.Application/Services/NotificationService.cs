using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Common.Models;
using LaoCai.SoftwareManagement.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationService(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.RecipientUserId,
                n.Type,
                n.Title,
                n.Message,
                n.TargetRoute,
                n.ReadAt,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && n.ReadAt == null, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, cancellationToken);

        if (notif == null)
            return false;

        if (notif.ReadAt == null)
        {
            notif.ReadAt = _dateTimeProvider.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var unreadList = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (unreadList.Count == 0)
            return 0;

        foreach (var notif in unreadList)
        {
            notif.ReadAt = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unreadList.Count;
    }

    public async Task<Guid> CreateNotificationAsync(
        Guid recipientUserId,
        string type,
        string title,
        string message,
        string? targetRoute = null,
        string? deduplicationKey = null,
        CancellationToken cancellationToken = default)
    {
        // Check deduplication
        if (!string.IsNullOrWhiteSpace(deduplicationKey))
        {
            var exists = await _context.Notifications
                .AnyAsync(n => n.DeduplicationKey == deduplicationKey, cancellationToken);

            if (exists)
            {
                // Already created, return existing
                var existingNotif = await _context.Notifications
                    .FirstAsync(n => n.DeduplicationKey == deduplicationKey, cancellationToken);
                return existingNotif.Id;
            }
        }

        var notif = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            TargetRoute = targetRoute,
            DeduplicationKey = deduplicationKey,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync(cancellationToken);
        return notif.Id;
    }
}
