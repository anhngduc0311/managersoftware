using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Reports;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _context;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardService(
        IAppDbContext context,
        IScopeAuthorizationService scopeAuth,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _scopeAuth = scopeAuth;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var asOf = filter.AsOfDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var asOfUtcEnd = asOf.ToDateTime(new TimeOnly(23, 59, 59, 999), DateTimeKind.Utc);

        HashSet<Guid>? allowedOrgs = null;
        bool canReadContracts = false;

        if (_currentUser.UserId.HasValue)
        {
            var allowedReports = await _scopeAuth.GetAllowedOrganizationIdsAsync(_currentUser.UserId.Value, "reports.read", cancellationToken);
            var allowedDeployments = await _scopeAuth.GetAllowedOrganizationIdsAsync(_currentUser.UserId.Value, "deployments.read", cancellationToken);

            if (allowedReports == null || allowedDeployments == null)
            {
                allowedOrgs = null; // Global
            }
            else
            {
                allowedOrgs = (allowedReports ?? new HashSet<Guid>())
                    .Union(allowedDeployments ?? new HashSet<Guid>())
                    .ToHashSet();
            }

            var allowedContracts = await _scopeAuth.GetAllowedOrganizationIdsAsync(_currentUser.UserId.Value, "contracts.read", cancellationToken);
            canReadContracts = (allowedContracts == null || allowedContracts.Count > 0);
        }
        else
        {
            canReadContracts = true;
        }

        // Query Deployments
        var depQuery = _context.Deployments
            .AsNoTracking()
            .Include(d => d.Software)
            .Include(d => d.Organization)
                .ThenInclude(o => o.Versions)
            .Include(d => d.Revisions)
            .AsQueryable();

        if (allowedOrgs != null)
        {
            depQuery = depQuery.Where(d => allowedOrgs.Contains(d.OrganizationId));
        }

        if (filter.OrganizationId.HasValue)
        {
            depQuery = depQuery.Where(d => d.OrganizationId == filter.OrganizationId.Value);
        }

        if (filter.SoftwareId.HasValue)
        {
            depQuery = depQuery.Where(d => d.SoftwareId == filter.SoftwareId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            depQuery = depQuery.Where(d => d.Software.CategoryId == filter.CategoryId.Value);
        }

        var allDeps = await depQuery.ToListAsync(cancellationToken);

        // Find approved revision asOf
        var approvedDepsList = new List<(Domain.Entities.Deployments.Deployment Dep, Domain.Entities.Deployments.DeploymentRevision Rev)>();

        foreach (var dep in allDeps)
        {
            var revAsOf = dep.Revisions
                .Where(r => r.WorkflowStatus == "Approved" && r.ApprovedAt.HasValue && r.ApprovedAt.Value <= asOfUtcEnd)
                .OrderByDescending(r => r.RevisionNo)
                .FirstOrDefault();

            if (revAsOf != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.OperationalStatus) &&
                    !revAsOf.OperationalStatus.Equals(filter.OperationalStatus, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                approvedDepsList.Add((dep, revAsOf));
            }
        }

        // 1. Total Software Count (distinct software having approved deployment in scope)
        var totalSoftwareCount = approvedDepsList.Select(x => x.Dep.SoftwareId).Distinct().Count();

        // 2. Total Deployments Count (distinct deployments having approved revision)
        var totalDeploymentsCount = approvedDepsList.Select(x => x.Dep.Id).Distinct().Count();

        // 3. Active Organizations Count
        var activeOrgs = approvedDepsList
            .Where(x => x.Rev.OperationalStatus == "Active")
            .Select(x => x.Dep.OrganizationId)
            .Distinct()
            .ToHashSet();
        var activeOrganizationsCount = activeOrgs.Count;

        // 4. Pending Approval Count (Submitted revisions in scope as of now)
        var pendingApprovalCount = allDeps
            .SelectMany(d => d.Revisions)
            .Count(r => r.WorkflowStatus == "Submitted");

        // 5. Coverage Calculation
        var covQuery = _context.CoverageEligibilities
            .AsNoTracking()
            .Where(ce => ce.IsEligible && ce.ValidFrom <= asOf && (!ce.ValidTo.HasValue || ce.ValidTo.Value >= asOf));

        if (allowedOrgs != null)
        {
            covQuery = covQuery.Where(ce => allowedOrgs.Contains(ce.OrganizationId));
        }

        if (filter.OrganizationId.HasValue)
        {
            covQuery = covQuery.Where(ce => ce.OrganizationId == filter.OrganizationId.Value);
        }

        if (filter.SoftwareId.HasValue)
        {
            covQuery = covQuery.Where(ce => ce.SoftwareId == filter.SoftwareId.Value);
        }

        var eligibleOrgIds = await covQuery
            .Select(ce => ce.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var eligibleOrganizationsCount = eligibleOrgIds.Count;
        var eligibleActiveCount = eligibleOrgIds.Count(id => activeOrgs.Contains(id));

        double? coveragePercentage = null;
        if (eligibleOrganizationsCount > 0)
        {
            coveragePercentage = Math.Round((double)eligibleActiveCount / eligibleOrganizationsCount * 100, 1);
        }

        // 6. Contracts KPI
        int expiringContractsCount = 0;
        decimal? totalContractAmount = null;

        if (canReadContracts)
        {
            var contractQuery = _context.Contracts
                .AsNoTracking()
                .Where(c => c.Status == "Active" && c.StartDate <= asOf && c.EndDate >= asOf);

            if (allowedOrgs != null)
            {
                contractQuery = contractQuery.Where(c => allowedOrgs.Contains(c.OwningOrganizationId));
            }

            if (filter.OrganizationId.HasValue)
            {
                contractQuery = contractQuery.Where(c => c.OwningOrganizationId == filter.OrganizationId.Value);
            }

            var activeContracts = await contractQuery.ToListAsync(cancellationToken);
            var expiryThreshold = asOf.AddDays(90);

            expiringContractsCount = activeContracts.Count(c => c.EndDate <= expiryThreshold);
            totalContractAmount = activeContracts.Sum(c => c.TotalAmount);
        }

        // Breakdowns
        var orgBreakdown = approvedDepsList
            .GroupBy(x => new
            {
                x.Dep.OrganizationId,
                Name = x.Dep.Organization.Versions
                    .Where(v => v.ValidFrom <= asOfUtcEnd && (!v.ValidTo.HasValue || v.ValidTo.Value >= asOfUtcEnd))
                    .OrderByDescending(v => v.ValidFrom)
                    .Select(v => v.Name)
                    .FirstOrDefault() ?? x.Dep.Organization.Code
            })
            .Select(g => new OrgDeploymentStatDto(
                g.Key.OrganizationId,
                g.Key.Name,
                g.Count(),
                g.Count(x => x.Rev.OperationalStatus == "Active")
            ))
            .OrderByDescending(x => x.DeploymentCount)
            .Take(10)
            .ToList();

        var statusBreakdown = approvedDepsList
            .GroupBy(x => x.Rev.OperationalStatus)
            .Select(g => new StatusStatDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var environmentBreakdown = approvedDepsList
            .GroupBy(x => x.Dep.Environment)
            .Select(g => new EnvironmentStatDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new DashboardKpiDto(
            totalSoftwareCount,
            totalDeploymentsCount,
            activeOrganizationsCount,
            coveragePercentage,
            eligibleOrganizationsCount,
            eligibleActiveCount,
            pendingApprovalCount,
            expiringContractsCount,
            totalContractAmount,
            "VND",
            asOf,
            _dateTimeProvider.UtcNow,
            orgBreakdown,
            statusBreakdown,
            environmentBreakdown
        );
    }

    public async Task<List<CoverageEligibilityDto>> GetCoverageEligibilitiesAsync(Guid? softwareId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CoverageEligibilities
            .AsNoTracking()
            .Include(ce => ce.Organization)
                .ThenInclude(o => o.Versions)
            .Include(ce => ce.Software)
            .AsQueryable();

        if (softwareId.HasValue)
        {
            query = query.Where(ce => ce.SoftwareId == softwareId.Value);
        }

        var items = await query.OrderBy(ce => ce.ValidFrom).ToListAsync(cancellationToken);

        return items.Select(ce => new CoverageEligibilityDto(
            ce.Id,
            ce.OrganizationId,
            ce.Organization.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? ce.Organization.Code,
            ce.SoftwareId,
            ce.Software.Name,
            ce.ValidFrom,
            ce.ValidTo,
            ce.IsEligible,
            ce.Note
        )).ToList();
    }

    public async Task<CoverageEligibilityDto> SetCoverageEligibilityAsync(CreateCoverageEligibilityRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(_currentUser.UserId.Value, "reports.read", null, cancellationToken);
            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền quản lý cấu hình độ phủ hệ thống.");
        }

        var orgExists = await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);
        if (!orgExists)
            throw new NotFoundException("Đơn vị", request.OrganizationId);

        var swExists = await _context.Software.AnyAsync(s => s.Id == request.SoftwareId, cancellationToken);
        if (!swExists)
            throw new NotFoundException("Phần mềm", request.SoftwareId);

        var eligibility = new CoverageEligibility
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            SoftwareId = request.SoftwareId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsEligible = request.IsEligible,
            Note = request.Note,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        eligibility.Validate();

        _context.CoverageEligibilities.Add(eligibility);
        await _context.SaveChangesAsync(cancellationToken);

        var orgName = await _context.OrganizationVersions
            .Where(v => v.OrganizationId == request.OrganizationId)
            .OrderByDescending(v => v.ValidFrom)
            .Select(v => v.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var swName = await _context.Software
            .Where(s => s.Id == request.SoftwareId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new CoverageEligibilityDto(
            eligibility.Id,
            eligibility.OrganizationId,
            orgName,
            eligibility.SoftwareId,
            swName,
            eligibility.ValidFrom,
            eligibility.ValidTo,
            eligibility.IsEligible,
            eligibility.Note
        );
    }
}
