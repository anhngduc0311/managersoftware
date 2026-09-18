using LaoCai.SoftwareManagement.Domain.Entities.Catalog;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public interface ICatalogService
{
    Task<bool> IsSoftwareCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsReleaseVersionUniqueAsync(Guid softwareId, string versionName, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Software> AcceptProposalAsync(Guid proposalId, Guid reviewerUserId, Guid? existingSoftwareId, string? newSoftwareCode, Guid? categoryId, Guid? vendorId, CancellationToken cancellationToken = default);
    Task RejectProposalAsync(Guid proposalId, Guid reviewerUserId, string reason, CancellationToken cancellationToken = default);
}
