namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public class UserGrantDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Organization";
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public bool IncludeDescendants { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public interface IScopeAuthorizationService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? targetOrgId = null, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>?> GetAllowedOrganizationIdsAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<List<UserGrantDto>> GetUserActiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default);
}
