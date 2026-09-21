using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class LicenseAllocationService : ILicenseAllocationService
{
    private readonly IAppDbContext _context;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly ICurrentUserService _currentUser;

    public LicenseAllocationService(
        IAppDbContext context,
        IScopeAuthorizationService scopeAuth,
        ICurrentUserService currentUser)
    {
        _context = context;
        _scopeAuth = scopeAuth;
        _currentUser = currentUser;
    }

    public async Task<List<LicenseAllocationDto>> GetAllocationsByEntitlementAsync(Guid entitlementId, CancellationToken cancellationToken = default)
    {
        var allocations = await _context.LicenseAllocations
            .AsNoTracking()
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Software)
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Organization)
                    .ThenInclude(o => o.Versions)
            .Where(a => a.EntitlementId == entitlementId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(cancellationToken);

        return allocations.Select(a => MapToDto(a)).ToList();
    }

    public async Task<List<LicenseAllocationDto>> GetAllocationsByDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default)
    {
        var allocations = await _context.LicenseAllocations
            .AsNoTracking()
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Software)
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Organization)
                    .ThenInclude(o => o.Versions)
            .Where(a => a.DeploymentId == deploymentId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(cancellationToken);

        return allocations.Select(a => MapToDto(a)).ToList();
    }

    public async Task<LicenseAllocationDto> AllocateLicenseAsync(CreateAllocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new CustomValidationException("quantity", "Số lượng license phân bổ phải lớn hơn 0.");

        var deployment = await _context.Deployments
            .Include(d => d.Software)
            .Include(d => d.Organization)
                .ThenInclude(o => o.Versions)
            .FirstOrDefaultAsync(d => d.Id == request.DeploymentId, cancellationToken);

        if (deployment == null)
            throw new NotFoundException("Hồ sơ triển khai", request.DeploymentId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "licenses.allocate",
                deployment.OrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền phân bổ license cho đơn vị của hồ sơ triển khai này.");
        }

        // Lock row if relational database, or standard include if in-memory
        LicenseEntitlement? entitlement;
        if (_context is DbContext dbContext && dbContext.Database.IsRelational())
        {
            entitlement = await _context.LicenseEntitlements
                .FromSqlRaw("SELECT * FROM contracts.license_entitlements WHERE id = {0} FOR UPDATE", request.EntitlementId)
                .Include(e => e.ContractItem)
                    .ThenInclude(ci => ci.Contract)
                .Include(e => e.Allocations)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            entitlement = await _context.LicenseEntitlements
                .Include(e => e.ContractItem)
                    .ThenInclude(ci => ci.Contract)
                .Include(e => e.Allocations)
                .FirstOrDefaultAsync(e => e.Id == request.EntitlementId, cancellationToken);
        }

        if (entitlement == null)
            throw new NotFoundException("Hạn mức license (Entitlement)", request.EntitlementId);

        if (deployment.SoftwareId != entitlement.ContractItem.SoftwareId)
        {
            throw new CustomValidationException("deploymentId", "Phần mềm của hồ sơ triển khai không khớp với phần mềm của hợp đồng.");
        }

        // Quota check
        if (entitlement.LicenseType.Equals("Seat", StringComparison.OrdinalIgnoreCase))
        {
            var currentAllocated = entitlement.Allocations
                .Where(a => a.DeploymentId != request.DeploymentId)
                .Sum(a => a.Quantity);

            var maxQuota = entitlement.Quantity ?? 0;
            if (currentAllocated + request.Quantity > maxQuota)
            {
                var remaining = Math.Max(0, maxQuota - currentAllocated);
                throw new CustomValidationException("quantity", $"Số lượng license yêu cầu ({request.Quantity}) vượt quá hạn mức còn lại của hợp đồng (Còn lại: {remaining} seats).");
            }
        }

        var existingAllocation = await _context.LicenseAllocations
            .FirstOrDefaultAsync(a => a.EntitlementId == request.EntitlementId && a.DeploymentId == request.DeploymentId, cancellationToken);

        if (existingAllocation != null)
        {
            existingAllocation.Quantity = request.Quantity;
            existingAllocation.AllocatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapToDto(existingAllocation, deployment);
        }

        var allocation = new LicenseAllocation
        {
            Id = Guid.NewGuid(),
            EntitlementId = request.EntitlementId,
            DeploymentId = request.DeploymentId,
            Quantity = request.Quantity,
            AllocatedAt = DateTime.UtcNow
        };

        allocation.Validate();

        _context.LicenseAllocations.Add(allocation);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(allocation, deployment);
    }

    public async Task<LicenseAllocationDto> UpdateAllocationAsync(Guid allocationId, UpdateAllocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new CustomValidationException("quantity", "Số lượng license phân bổ phải lớn hơn 0.");

        var allocation = await _context.LicenseAllocations
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Software)
            .Include(a => a.Deployment)
                .ThenInclude(d => d.Organization)
                    .ThenInclude(o => o.Versions)
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);

        if (allocation == null)
            throw new NotFoundException("Bản ghi phân bổ license", allocationId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "licenses.allocate",
                allocation.Deployment.OrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền điều chỉnh phân bổ license cho đơn vị này.");
        }

        // Lock row if relational
        LicenseEntitlement? entitlement;
        if (_context is DbContext dbContext && dbContext.Database.IsRelational())
        {
            entitlement = await _context.LicenseEntitlements
                .FromSqlRaw("SELECT * FROM contracts.license_entitlements WHERE id = {0} FOR UPDATE", allocation.EntitlementId)
                .Include(e => e.Allocations)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            entitlement = await _context.LicenseEntitlements
                .Include(e => e.Allocations)
                .FirstOrDefaultAsync(e => e.Id == allocation.EntitlementId, cancellationToken);
        }

        if (entitlement != null && entitlement.LicenseType.Equals("Seat", StringComparison.OrdinalIgnoreCase))
        {
            var currentAllocatedExceptThis = entitlement.Allocations
                .Where(a => a.Id != allocationId)
                .Sum(a => a.Quantity);

            var maxQuota = entitlement.Quantity ?? 0;
            if (currentAllocatedExceptThis + request.Quantity > maxQuota)
            {
                var remaining = Math.Max(0, maxQuota - currentAllocatedExceptThis);
                throw new CustomValidationException("quantity", $"Số lượng license yêu cầu ({request.Quantity}) vượt quá hạn mức còn lại của hợp đồng (Còn lại: {remaining} seats).");
            }
        }

        allocation.Quantity = request.Quantity;
        allocation.AllocatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(allocation);
    }

    public async Task RevokeAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default)
    {
        var allocation = await _context.LicenseAllocations
            .Include(a => a.Deployment)
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);

        if (allocation == null)
            throw new NotFoundException("Bản ghi phân bổ license", allocationId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "licenses.allocate",
                allocation.Deployment.OrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền thu hồi phân bổ license của đơn vị này.");
        }

        _context.LicenseAllocations.Remove(allocation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static LicenseAllocationDto MapToDto(LicenseAllocation a, LaoCai.SoftwareManagement.Domain.Entities.Deployments.Deployment? dep = null)
    {
        var deployment = dep ?? a.Deployment;
        return new LicenseAllocationDto(
            a.Id,
            a.EntitlementId,
            a.DeploymentId,
            deployment?.Software?.Name ?? string.Empty,
            deployment?.Organization?.Versions?.FirstOrDefault(v => v.ValidTo == null)?.Name ?? deployment?.Organization?.Code ?? string.Empty,
            deployment?.Environment ?? string.Empty,
            deployment?.InstanceKey ?? string.Empty,
            a.Quantity,
            a.AllocatedAt);
    }
}
