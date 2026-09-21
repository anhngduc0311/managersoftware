using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record DeploymentFilterDto(
    Guid? OrganizationId = null,
    Guid? SoftwareId = null,
    string? Environment = null,
    string? OperationalStatus = null,
    string? WorkflowStatus = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public record DeploymentDto(
    Guid Id,
    Guid SoftwareId,
    string SoftwareCode,
    string SoftwareName,
    Guid OrganizationId,
    string OrganizationCode,
    string OrganizationName,
    string Environment,
    string InstanceKey,
    Guid? CurrentApprovedRevisionId,
    DeploymentRevisionDto? CurrentApprovedRevision,
    DeploymentRevisionDto? ActiveRevision,
    long Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record DeploymentRevisionDto(
    Guid Id,
    Guid DeploymentId,
    int RevisionNo,
    Guid? ReleaseId,
    string? ReleaseVersionName,
    string OperationalStatus,
    DateOnly? StartDate,
    DateOnly? GoLiveDate,
    Guid? ResponsibleUserId,
    string? ResponsibleUserName,
    string? ResponsibleUserDisplayName,
    string WorkflowStatus,
    Guid? SubmittedBy,
    string? SubmittedByUserName,
    string? SubmittedByDisplayName,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    long Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ApprovalDecisionDto>? Decisions = null);

public record ApprovalDecisionDto(
    Guid Id,
    Guid DeploymentRevisionId,
    string Decision,
    string? Reason,
    Guid ActorId,
    string ActorUserName,
    string ActorDisplayName,
    DateTime DecidedAt);

public record CreateDeploymentDto(
    Guid SoftwareId,
    Guid OrganizationId,
    string Environment,
    string InstanceKey,
    Guid? ReleaseId,
    string OperationalStatus,
    DateOnly? StartDate,
    DateOnly? GoLiveDate,
    Guid? ResponsibleUserId);

public record UpdateDraftRevisionDto(
    Guid? ReleaseId,
    string OperationalStatus,
    DateOnly? StartDate,
    DateOnly? GoLiveDate,
    Guid? ResponsibleUserId);

public record RejectRevisionDto(string Reason);

public interface IDeploymentService
{
    Task<PagedResult<DeploymentDto>> GetDeploymentsAsync(DeploymentFilterDto filter, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentDto> GetDeploymentByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> GetRevisionByIdAsync(Guid revisionId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<List<DeploymentRevisionDto>> GetRevisionHistoryAsync(Guid deploymentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentDto> CreateDeploymentAsync(CreateDeploymentDto dto, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> UpdateDraftRevisionAsync(Guid revisionId, UpdateDraftRevisionDto dto, long ifMatchVersion, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> CreateNextRevisionAsync(Guid deploymentId, long ifMatchDeploymentVersion, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> ReopenRejectedRevisionAsync(Guid revisionId, long ifMatchVersion, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> SubmitRevisionAsync(Guid revisionId, long ifMatchVersion, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> ApproveRevisionAsync(Guid revisionId, long ifMatchVersion, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<DeploymentRevisionDto> RejectRevisionAsync(Guid revisionId, RejectRevisionDto dto, long ifMatchVersion, Guid currentUserId, CancellationToken cancellationToken = default);
}
