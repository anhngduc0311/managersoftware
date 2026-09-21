using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record DashboardFilter(
    Guid? OrganizationId = null,
    Guid? SoftwareId = null,
    Guid? CategoryId = null,
    string? OperationalStatus = null,
    DateOnly? AsOfDate = null
);

public record DashboardKpiDto(
    int TotalSoftwareCount,
    int TotalDeploymentsCount,
    int ActiveOrganizationsCount,
    double? CoveragePercentage,
    int EligibleOrganizationsCount,
    int EligibleActiveOrganizationsCount,
    int PendingApprovalCount,
    int ExpiringContractsCount,
    decimal? TotalContractAmount,
    string CurrencyCode,
    DateOnly AsOfDate,
    DateTime GeneratedAtUtc,
    List<OrgDeploymentStatDto> OrgBreakdown,
    List<StatusStatDto> StatusBreakdown,
    List<EnvironmentStatDto> EnvironmentBreakdown
);

public record OrgDeploymentStatDto(
    Guid OrganizationId,
    string OrganizationName,
    int DeploymentCount,
    int ActiveCount
);

public record StatusStatDto(
    string Status,
    int Count
);

public record EnvironmentStatDto(
    string Environment,
    int Count
);

public record CoverageEligibilityDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    Guid SoftwareId,
    string SoftwareName,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsEligible,
    string? Note
);

public record CreateCoverageEligibilityRequest(
    Guid OrganizationId,
    Guid SoftwareId,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsEligible = true,
    string? Note = null
);

public interface IDashboardService
{
    Task<DashboardKpiDto> GetDashboardKpisAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
    Task<List<CoverageEligibilityDto>> GetCoverageEligibilitiesAsync(Guid? softwareId = null, CancellationToken cancellationToken = default);
    Task<CoverageEligibilityDto> SetCoverageEligibilityAsync(CreateCoverageEligibilityRequest request, CancellationToken cancellationToken = default);
}
