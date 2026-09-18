using LaoCai.SoftwareManagement.Domain.Common;

namespace LaoCai.SoftwareManagement.Domain.Entities.Organizations;

public class Organization : Entity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<OrganizationVersion> Versions { get; set; } = new List<OrganizationVersion>();
}

public class OrganizationVersion : Entity<Guid>, IAuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Organization? Parent { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsEffectiveAt(DateTime asOfUtc)
    {
        return ValidFrom <= asOfUtc && (ValidTo == null || ValidTo > asOfUtc);
    }
}

public class OrganizationSuccession : Entity<Guid>, IAuditableEntity
{
    public Guid PredecessorId { get; set; }
    public Organization Predecessor { get; set; } = null!;

    public Guid SuccessorId { get; set; }
    public Organization Successor { get; set; } = null!;

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
