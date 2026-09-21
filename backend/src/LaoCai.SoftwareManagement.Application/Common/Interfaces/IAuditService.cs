using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record AuditLogDto(
    Guid Id,
    Guid? ActorId,
    string? ActorName,
    string Action,
    string EntityType,
    string EntityId,
    Guid? OrganizationId,
    string? OrganizationName,
    string? BeforeJson,
    string? AfterJson,
    DateTime OccurredAt,
    string? CorrelationId);

public record AuditFilterDto(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? Action = null,
    string? SearchTerm = null,
    Guid? OrganizationId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? CorrelationId = null);

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(Guid currentUserId, AuditFilterDto filter, CancellationToken cancellationToken = default);
    Task<AuditLogDto?> GetAuditLogByIdAsync(Guid logId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Guid> LogActivityAsync(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        Guid? organizationId,
        string? beforeJson,
        string? afterJson,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
