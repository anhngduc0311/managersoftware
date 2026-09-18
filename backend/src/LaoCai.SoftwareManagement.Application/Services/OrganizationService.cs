using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OrganizationService(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> HasCycleAsync(
        Guid orgId,
        Guid? proposedParentId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        if (!proposedParentId.HasValue)
            return false;

        if (proposedParentId.Value == orgId)
            return true;

        // Fetch all active versions at asOfUtc
        var activeVersions = await _context.OrganizationVersions
            .AsNoTracking()
            .Where(v => v.ValidFrom <= asOfUtc && (v.ValidTo == null || v.ValidTo > asOfUtc))
            .Select(v => new { v.OrganizationId, v.ParentId })
            .ToListAsync(cancellationToken);

        var parentMap = activeVersions
            .GroupBy(v => v.OrganizationId)
            .ToDictionary(g => g.Key, g => g.First().ParentId);

        var currentParentId = proposedParentId;
        var visited = new HashSet<Guid> { orgId };

        while (currentParentId.HasValue)
        {
            if (visited.Contains(currentParentId.Value))
            {
                return true; // Cycle detected!
            }

            visited.Add(currentParentId.Value);

            if (!parentMap.TryGetValue(currentParentId.Value, out currentParentId))
            {
                break;
            }
        }

        return false;
    }

    public async Task<bool> HasDateOverlapAsync(
        Guid orgId,
        DateTime validFrom,
        DateTime? validTo,
        Guid? excludeVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveEnd = validTo ?? DateTime.MaxValue;

        var existingVersions = await _context.OrganizationVersions
            .AsNoTracking()
            .Where(v => v.OrganizationId == orgId && (excludeVersionId == null || v.Id != excludeVersionId))
            .ToListAsync(cancellationToken);

        foreach (var v in existingVersions)
        {
            var vEnd = v.ValidTo ?? DateTime.MaxValue;
            // Overlap check: start1 < end2 && end1 > start2
            if (validFrom < vEnd && effectiveEnd > v.ValidFrom)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<HashSet<Guid>> GetDescendantOrgIdsAsync(
        Guid orgId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var activeVersions = await _context.OrganizationVersions
            .AsNoTracking()
            .Where(v => v.ValidFrom <= asOfUtc && (v.ValidTo == null || v.ValidTo > asOfUtc))
            .Select(v => new { v.OrganizationId, v.ParentId })
            .ToListAsync(cancellationToken);

        var childrenMap = activeVersions
            .Where(v => v.ParentId.HasValue)
            .GroupBy(v => v.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.OrganizationId).ToList());

        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(orgId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (childrenMap.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    if (result.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        return result;
    }

    public async Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = asOfUtc ?? _dateTimeProvider.UtcNow;

        var orgs = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Versions)
            .ToListAsync(cancellationToken);

        var nodes = new List<OrganizationTreeNodeDto>();

        foreach (var org in orgs)
        {
            var version = org.Versions
                .Where(v => v.ValidFrom <= effectiveDate && (v.ValidTo == null || v.ValidTo > effectiveDate))
                .OrderByDescending(v => v.ValidFrom)
                .FirstOrDefault();

            if (version == null)
            {
                // Fallback to most recent version
                version = org.Versions.OrderByDescending(v => v.ValidFrom).FirstOrDefault();
            }

            if (version != null)
            {
                nodes.Add(new OrganizationTreeNodeDto
                {
                    Id = org.Id,
                    Code = org.Code,
                    Name = version.Name,
                    ParentId = version.ParentId,
                    IsActive = org.IsActive,
                    ValidFrom = version.ValidFrom,
                    ValidTo = version.ValidTo,
                    Children = new List<OrganizationTreeNodeDto>()
                });
            }
        }

        var nodeMap = nodes.ToDictionary(n => n.Id);
        var rootNodes = new List<OrganizationTreeNodeDto>();

        foreach (var node in nodes)
        {
            if (node.ParentId.HasValue && nodeMap.TryGetValue(node.ParentId.Value, out var parentNode))
            {
                parentNode.Children.Add(node);
            }
            else
            {
                rootNodes.Add(node);
            }
        }

        return rootNodes;
    }
}
