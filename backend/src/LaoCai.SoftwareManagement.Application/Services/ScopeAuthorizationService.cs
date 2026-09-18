using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class ScopeAuthorizationService : IScopeAuthorizationService
{
    private readonly IAppDbContext _context;
    private readonly IOrganizationService _organizationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ScopeAuthorizationService(
        IAppDbContext context,
        IOrganizationService organizationService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _organizationService = organizationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? targetOrgId = null,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var grants = await _context.UserRoleScopes
            .AsNoTracking()
            .Where(urs => urs.UserId == userId && urs.ValidFrom <= nowUtc && (urs.ValidTo == null || urs.ValidTo > nowUtc))
            .Include(urs => urs.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            var hasPermission = grant.Role.RolePermissions.Any(rp =>
                string.Equals(rp.Permission.Code, permissionCode, StringComparison.OrdinalIgnoreCase));

            if (!hasPermission)
                continue;

            // Global scope satisfies any target organization
            if (string.Equals(grant.ScopeType, "Global", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // If no specific target org is required for the check, having the grant in an org is sufficient
            if (!targetOrgId.HasValue)
            {
                return true;
            }

            // Direct organization match
            if (grant.OrganizationId.HasValue && grant.OrganizationId.Value == targetOrgId.Value)
            {
                return true;
            }

            // Descendants match
            if (grant.IncludeDescendants && grant.OrganizationId.HasValue)
            {
                var descendantIds = await _organizationService.GetDescendantOrgIdsAsync(
                    grant.OrganizationId.Value,
                    nowUtc,
                    cancellationToken);

                if (descendantIds.Contains(targetOrgId.Value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<HashSet<Guid>?> GetAllowedOrganizationIdsAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var grants = await _context.UserRoleScopes
            .AsNoTracking()
            .Where(urs => urs.UserId == userId && urs.ValidFrom <= nowUtc && (urs.ValidTo == null || urs.ValidTo > nowUtc))
            .Include(urs => urs.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var allowedOrgIds = new HashSet<Guid>();

        foreach (var grant in grants)
        {
            var hasPermission = grant.Role.RolePermissions.Any(rp =>
                string.Equals(rp.Permission.Code, permissionCode, StringComparison.OrdinalIgnoreCase));

            if (!hasPermission)
                continue;

            // Global scope means all organizations
            if (string.Equals(grant.ScopeType, "Global", StringComparison.OrdinalIgnoreCase))
            {
                return null; // Represents all orgs
            }

            if (grant.OrganizationId.HasValue)
            {
                allowedOrgIds.Add(grant.OrganizationId.Value);

                if (grant.IncludeDescendants)
                {
                    var descendants = await _organizationService.GetDescendantOrgIdsAsync(
                        grant.OrganizationId.Value,
                        nowUtc,
                        cancellationToken);

                    foreach (var descId in descendants)
                    {
                        allowedOrgIds.Add(descId);
                    }
                }
            }
        }

        return allowedOrgIds;
    }

    public async Task<List<UserGrantDto>> GetUserActiveGrantsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;

        var grants = await _context.UserRoleScopes
            .AsNoTracking()
            .Where(urs => urs.UserId == userId && urs.ValidFrom <= nowUtc && (urs.ValidTo == null || urs.ValidTo > nowUtc))
            .Include(urs => urs.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        var orgIds = grants.Where(g => g.OrganizationId.HasValue).Select(g => g.OrganizationId!.Value).Distinct().ToList();

        var orgVersions = await _context.OrganizationVersions
            .AsNoTracking()
            .Where(v => orgIds.Contains(v.OrganizationId) && v.ValidFrom <= nowUtc && (v.ValidTo == null || v.ValidTo > nowUtc))
            .ToListAsync(cancellationToken);

        var orgNameMap = orgVersions
            .GroupBy(v => v.OrganizationId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var result = new List<UserGrantDto>();

        foreach (var grant in grants)
        {
            string? orgName = null;
            if (grant.OrganizationId.HasValue && orgNameMap.TryGetValue(grant.OrganizationId.Value, out var name))
            {
                orgName = name;
            }

            result.Add(new UserGrantDto
            {
                Id = grant.Id,
                RoleId = grant.RoleId,
                RoleCode = grant.Role.Code,
                RoleName = grant.Role.Name,
                ScopeType = grant.ScopeType,
                OrganizationId = grant.OrganizationId,
                OrganizationName = orgName,
                IncludeDescendants = grant.IncludeDescendants,
                ValidFrom = grant.ValidFrom,
                ValidTo = grant.ValidTo,
                Permissions = grant.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList()
            });
        }

        return result;
    }
}
