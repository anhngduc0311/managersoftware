using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;

namespace LaoCai.SoftwareManagement.Domain.Entities.Catalog;

public class SoftwareCategory : Entity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<Software> SoftwareList { get; set; } = new List<Software>();
}

public class Vendor : Entity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<Software> SoftwareList { get; set; } = new List<Software>();
}

public class Software : Entity<Guid>, IAuditableEntity, IVersionedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public SoftwareCategory Category { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public string? Description { get; set; }
    public string LifecycleStatus { get; set; } = "Active"; // Active | Deprecated | Retired
    public long Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<SoftwareRelease> Releases { get; set; } = new List<SoftwareRelease>();
}

public class SoftwareRelease : Entity<Guid>, IAuditableEntity
{
    public Guid SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public string VersionName { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? SupportEndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public class CatalogProposal : Entity<Guid>, IAuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid ProposedByUserId { get; set; }
    public User ProposedByUser { get; set; } = null!;

    public string SoftwareName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Accepted | Rejected
    public string? RejectionReason { get; set; }

    public Guid? CreatedSoftwareId { get; set; }
    public Software? CreatedSoftware { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
