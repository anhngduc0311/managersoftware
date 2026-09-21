namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record LicenseAllocationDto(
    Guid Id,
    Guid EntitlementId,
    Guid DeploymentId,
    string SoftwareName,
    string OrganizationName,
    string Environment,
    string InstanceKey,
    int Quantity,
    DateTime AllocatedAt
);

public record CreateAllocationRequest(
    Guid EntitlementId,
    Guid DeploymentId,
    int Quantity
);

public record UpdateAllocationRequest(
    int Quantity
);

public interface ILicenseAllocationService
{
    Task<List<LicenseAllocationDto>> GetAllocationsByEntitlementAsync(Guid entitlementId, CancellationToken cancellationToken = default);
    Task<List<LicenseAllocationDto>> GetAllocationsByDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<LicenseAllocationDto> AllocateLicenseAsync(CreateAllocationRequest request, CancellationToken cancellationToken = default);
    Task<LicenseAllocationDto> UpdateAllocationAsync(Guid allocationId, UpdateAllocationRequest request, CancellationToken cancellationToken = default);
    Task RevokeAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
}
