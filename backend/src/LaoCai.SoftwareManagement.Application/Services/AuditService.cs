using System.Text.Json;
using System.Text.Json.Nodes;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Common.Models;
using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _context;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly IDateTimeProvider _dateTimeProvider;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "SecurityStamp", "Token", "Secret", "PrivateKey", "Credential"
    };

    public AuditService(
        IAppDbContext context,
        IScopeAuthorizationService scopeAuth,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _scopeAuth = scopeAuth;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid currentUserId,
        AuditFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // 1. Authorization check
        var hasReadAudit = await _scopeAuth.HasPermissionAsync(currentUserId, "audit.read", null, cancellationToken);
        if (!hasReadAudit)
        {
            throw new ForbiddenException("Bạn không có quyền tra cứu nhật ký kiểm toán (audit.read).");
        }

        var allowedOrgIds = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId, "audit.read", cancellationToken);

        var query = _context.AuditLogs.AsNoTracking();

        // 2. Scope restriction
        if (allowedOrgIds != null)
        {
            query = query.Where(a => a.OrganizationId.HasValue && allowedOrgIds.Contains(a.OrganizationId.Value));
        }

        // 3. Filters
        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(a => a.OrganizationId == filter.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            query = query.Where(a => a.CorrelationId == filter.CorrelationId.Trim());
        }

        if (filter.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.OccurredAt >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.OccurredAt <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.EntityType.ToLower().Contains(term) ||
                a.EntityId.ToLower().Contains(term) ||
                a.Action.ToLower().Contains(term) ||
                (a.CorrelationId != null && a.CorrelationId.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var rawLogs = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Lookup actors and organizations
        var actorIds = rawLogs.Where(l => l.ActorId.HasValue).Select(l => l.ActorId!.Value).Distinct().ToList();
        var orgIds = rawLogs.Where(l => l.OrganizationId.HasValue).Select(l => l.OrganizationId!.Value).Distinct().ToList();

        var usersDict = await _context.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var allOrgVersions = await _context.OrganizationVersions.AsNoTracking()
            .Where(v => orgIds.Contains(v.OrganizationId) && v.ValidTo == null)
            .ToListAsync(cancellationToken);

        var orgsDict = allOrgVersions
            .GroupBy(v => v.OrganizationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.ValidFrom).First().Name);

        var dtos = rawLogs.Select(l =>
        {
            string? actorName = l.ActorId.HasValue && usersDict.TryGetValue(l.ActorId.Value, out var uName) ? uName : null;
            string? orgName = l.OrganizationId.HasValue && orgsDict.TryGetValue(l.OrganizationId.Value, out var oName) ? oName : null;

            return new AuditLogDto(
                l.Id,
                l.ActorId,
                actorName,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.OrganizationId,
                orgName,
                ScrubJson(l.BeforeJson),
                ScrubJson(l.AfterJson),
                l.OccurredAt,
                l.CorrelationId);
        }).ToList();

        return PagedResult<AuditLogDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(
        Guid logId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var hasReadAudit = await _scopeAuth.HasPermissionAsync(currentUserId, "audit.read", null, cancellationToken);
        if (!hasReadAudit)
        {
            throw new ForbiddenException("Bạn không có quyền tra cứu nhật ký kiểm toán (audit.read).");
        }

        var log = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);
        if (log == null)
            return null;

        var allowedOrgIds = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId, "audit.read", cancellationToken);
        if (allowedOrgIds != null && log.OrganizationId.HasValue && !allowedOrgIds.Contains(log.OrganizationId.Value))
        {
            throw new ForbiddenException("Bản ghi nhật ký kiểm toán nằm ngoài phạm vi đơn vị được phân quyền.");
        }

        string? actorName = null;
        if (log.ActorId.HasValue)
        {
            actorName = await _context.Users.AsNoTracking()
                .Where(u => u.Id == log.ActorId.Value)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? orgName = null;
        if (log.OrganizationId.HasValue)
        {
            orgName = await _context.OrganizationVersions.AsNoTracking()
                .Where(o => o.OrganizationId == log.OrganizationId.Value && o.ValidTo == null)
                .OrderByDescending(o => o.ValidFrom)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AuditLogDto(
            log.Id,
            log.ActorId,
            actorName,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.OrganizationId,
            orgName,
            ScrubJson(log.BeforeJson),
            ScrubJson(log.AfterJson),
            log.OccurredAt,
            log.CorrelationId);
    }

    public async Task<Guid> LogActivityAsync(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        Guid? organizationId,
        string? beforeJson,
        string? afterJson,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OrganizationId = organizationId,
            BeforeJson = ScrubJson(beforeJson),
            AfterJson = ScrubJson(afterJson),
            OccurredAt = _dateTimeProvider.UtcNow,
            CorrelationId = correlationId
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
        return log.Id;
    }

    public static string? ScrubJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null)
                return json;

            ScrubNode(node);
            return node.ToJsonString();
        }
        catch
        {
            return json;
        }
    }

    private static void ScrubNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var propsToRedact = new List<string>();
            foreach (var kv in obj)
            {
                if (SensitiveKeys.Contains(kv.Key))
                {
                    propsToRedact.Add(kv.Key);
                }
                else
                {
                    ScrubNode(kv.Value);
                }
            }

            foreach (var key in propsToRedact)
            {
                obj[key] = "***";
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                ScrubNode(item);
            }
        }
    }
}
