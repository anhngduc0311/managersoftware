using LaoCai.SoftwareManagement.Domain.Common;

namespace LaoCai.SoftwareManagement.Domain.Entities.Iam;

public class User : Entity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<UserRoleScope> RoleScopes { get; set; } = new List<UserRoleScope>();
}

public class Role : Entity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : Entity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class UserRoleScope : Entity<Guid>
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string ScopeType { get; set; } = "Organization"; // "Global" | "Organization"
    public Guid? OrganizationId { get; set; }
    public bool IncludeDescendants { get; set; } = false;

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    public bool IsCurrentlyValid(DateTime nowUtc)
    {
        return ValidFrom <= nowUtc && (ValidTo == null || ValidTo > nowUtc);
    }
}
