using LaoCai.SoftwareManagement.Domain.Common;

namespace LaoCai.SoftwareManagement.Domain.Entities.Audit;

public class AuditLog : Entity<Guid>
{
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Submit, Approve, Reject...
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }

    public static AuditLog Create(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        Guid? organizationId,
        string? beforeJson,
        string? afterJson,
        string? correlationId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OrganizationId = organizationId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };
    }
}
