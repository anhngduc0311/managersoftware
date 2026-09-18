using LaoCai.SoftwareManagement.Domain.Entities.Organizations;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public class OrganizationTreeNodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<OrganizationTreeNodeDto> Children { get; set; } = new();
}

public interface IOrganizationService
{
    Task<bool> HasCycleAsync(Guid orgId, Guid? proposedParentId, DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task<bool> HasDateOverlapAsync(Guid orgId, DateTime validFrom, DateTime? validTo, Guid? excludeVersionId = null, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetDescendantOrgIdsAsync(Guid orgId, DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync(DateTime? asOfUtc = null, CancellationToken cancellationToken = default);
}
