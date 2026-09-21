using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;

namespace LaoCai.SoftwareManagement.Domain.Entities.Deployments;

public class Deployment : Entity<Guid>, IAuditableEntity, IVersionedEntity
{
    public Guid SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Environment { get; set; } = "Production"; // Production | Test | Development
    public string InstanceKey { get; set; } = "default";

    public Guid? CurrentApprovedRevisionId { get; set; }
    public DeploymentRevision? CurrentApprovedRevision { get; set; }

    public long Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<DeploymentRevision> Revisions { get; set; } = new List<DeploymentRevision>();
}

public class DeploymentRevision : Entity<Guid>, IAuditableEntity, IVersionedEntity
{
    public Guid DeploymentId { get; set; }
    public Deployment Deployment { get; set; } = null!;

    public int RevisionNo { get; set; }

    public Guid? ReleaseId { get; set; }
    public SoftwareRelease? Release { get; set; }

    public string OperationalStatus { get; set; } = "NotInUse"; // NotInUse | Active | Suspended | Retired
    public DateOnly? StartDate { get; set; }
    public DateOnly? GoLiveDate { get; set; }

    public Guid? ResponsibleUserId { get; set; }
    public User? ResponsibleUser { get; set; }

    public string WorkflowStatus { get; set; } = "Draft"; // Draft | Submitted | Approved | Rejected

    public Guid? SubmittedBy { get; set; }
    public User? SubmittedByUser { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public long Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ApprovalDecision> Decisions { get; set; } = new List<ApprovalDecision>();
}

public class ApprovalDecision : Entity<Guid>
{
    public Guid DeploymentRevisionId { get; set; }
    public DeploymentRevision DeploymentRevision { get; set; } = null!;

    public string Decision { get; set; } = string.Empty; // Approved | Rejected
    public string? Reason { get; set; }

    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
