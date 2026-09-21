using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;

namespace LaoCai.SoftwareManagement.Domain.Entities.Notifications;

public class Notification : Entity<Guid>
{
    public Guid RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public string Type { get; set; } = string.Empty; // WorkflowSubmitted | WorkflowApproved | WorkflowRejected | System
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TargetRoute { get; set; }
    public string? DeduplicationKey { get; set; }

    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
