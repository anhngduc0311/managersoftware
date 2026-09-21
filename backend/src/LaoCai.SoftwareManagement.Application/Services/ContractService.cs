using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Common.Models;
using LaoCai.SoftwareManagement.Domain.Entities.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class ContractService : IContractService
{
    private readonly IAppDbContext _context;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly ICurrentUserService _currentUser;

    public ContractService(
        IAppDbContext context,
        IScopeAuthorizationService scopeAuth,
        ICurrentUserService currentUser)
    {
        _context = context;
        _scopeAuth = scopeAuth;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ContractDto>> GetContractsAsync(ContractFilter filter, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.UserId;
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        bool canReadFinancials = false;
        List<Guid>? allowedOrgs = null;

        if (currentUserId.HasValue)
        {
            var allowedOrgsRead = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId.Value, "contracts.read", cancellationToken);
            var allowedOrgsWrite = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId.Value, "contracts.write", cancellationToken);
            var allowedOrgsAlloc = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId.Value, "licenses.allocate", cancellationToken);
            var allowedOrgsDep = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId.Value, "deployments.read", cancellationToken);

            canReadFinancials = (allowedOrgsRead == null || allowedOrgsRead.Count > 0);

            if (allowedOrgsRead == null || allowedOrgsWrite == null)
            {
                allowedOrgs = null; // Global
            }
            else
            {
                var combined = allowedOrgsRead
                    .Union(allowedOrgsWrite)
                    .Union(allowedOrgsAlloc ?? Enumerable.Empty<Guid>())
                    .Union(allowedOrgsDep ?? Enumerable.Empty<Guid>())
                    .Distinct()
                    .ToList();

                if (combined.Count == 0)
                {
                    return PagedResult<ContractDto>.Create(new List<ContractDto>(), 0, page, pageSize);
                }
                allowedOrgs = combined;
            }
        }
        else
        {
            // Anonymous / testing without user
            canReadFinancials = true;
        }

        var query = _context.Contracts
            .AsNoTracking()
            .Include(c => c.OwningOrganization)
                .ThenInclude(o => o.Versions)
            .Include(c => c.Vendor)
            .Include(c => c.Items)
                .ThenInclude(i => i.Software)
            .Include(c => c.Items)
                .ThenInclude(i => i.Entitlements)
                    .ThenInclude(e => e.Allocations)
            .AsQueryable();

        if (allowedOrgs != null)
        {
            query = query.Where(c => allowedOrgs.Contains(c.OwningOrganizationId)
                || c.Items.Any(i => i.Entitlements.Any(e => e.Allocations.Any(a => allowedOrgs.Contains(a.Deployment.OrganizationId)))));
        }

        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(c => c.OwningOrganizationId == filter.OrganizationId.Value);
        }

        if (filter.VendorId.HasValue)
        {
            query = query.Where(c => c.VendorId == filter.VendorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(c => c.Status == filter.Status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.ContractNo.ToLower().Contains(s) ||
                c.OwningOrganization.Code.ToLower().Contains(s) ||
                c.Vendor.Name.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(c => MapToDto(c, canReadFinancials)).ToList();
        return PagedResult<ContractDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<(ContractDto Dto, string ETag)> GetContractByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.OwningOrganization)
                .ThenInclude(o => o.Versions)
            .Include(c => c.Vendor)
            .Include(c => c.Items)
                .ThenInclude(i => i.Software)
            .Include(c => c.Items)
                .ThenInclude(i => i.Entitlements)
                    .ThenInclude(e => e.Allocations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contract == null)
            throw new NotFoundException("Hợp đồng", id);

        bool canReadFinancials = true;
        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.read",
                contract.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
            {
                var isWriteAllowed = await _scopeAuth.HasPermissionAsync(
                    _currentUser.UserId.Value,
                    "contracts.write",
                    contract.OwningOrganizationId,
                    cancellationToken);

                var allowedAllocOrgs = await _scopeAuth.GetAllowedOrganizationIdsAsync(_currentUser.UserId.Value, "licenses.allocate", cancellationToken);
                var allowedDepOrgs = await _scopeAuth.GetAllowedOrganizationIdsAsync(_currentUser.UserId.Value, "deployments.read", cancellationToken);

                var hasOrgAccess = (allowedAllocOrgs == null || allowedDepOrgs == null);
                if (!hasOrgAccess)
                {
                    var userOrgs = (allowedAllocOrgs ?? new HashSet<Guid>()).Union(allowedDepOrgs ?? new HashSet<Guid>()).ToHashSet();
                    hasOrgAccess = userOrgs.Contains(contract.OwningOrganizationId) ||
                        contract.Items.Any(i => i.Entitlements.Any(e => e.Allocations.Any(a => userOrgs.Contains(a.Deployment.OrganizationId))));
                }

                if (!isWriteAllowed && !hasOrgAccess)
                    throw new ForbiddenException("Bạn không có quyền xem hợp đồng của đơn vị này.");

                canReadFinancials = false;
            }
        }

        var dto = MapToDto(contract, canReadFinancials);
        var etag = $"\"{contract.Version}\"";
        return (dto, etag);
    }

    public async Task<ContractDto> CreateContractAsync(CreateContractRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.write",
                request.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền tạo hợp đồng cho đơn vị này.");
        }

        var orgExists = await _context.Organizations.AnyAsync(o => o.Id == request.OwningOrganizationId, cancellationToken);
        if (!orgExists)
            throw new NotFoundException("Đơn vị sở hữu", request.OwningOrganizationId);

        var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == request.VendorId, cancellationToken);
        if (!vendorExists)
            throw new NotFoundException("Nhà cung cấp", request.VendorId);

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ContractNo = request.ContractNo.Trim(),
            OwningOrganizationId = request.OwningOrganizationId,
            VendorId = request.VendorId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            SignedDate = request.SignedDate,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = request.TotalAmount,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "VND" : request.CurrencyCode.Trim().ToUpper(),
            MaintenanceStartDate = request.MaintenanceStartDate,
            MaintenanceEndDate = request.MaintenanceEndDate,
            Version = 1
        };

        contract.Validate();

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetContractByIdAsync(contract.Id, cancellationToken)).Dto;
    }

    public async Task<ContractDto> UpdateContractAsync(Guid id, UpdateContractRequest request, long expectedVersion, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (contract == null)
            throw new NotFoundException("Hợp đồng", id);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.write",
                contract.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền cập nhật hợp đồng của đơn vị này.");
        }

        if (contract.Version != expectedVersion)
            throw new ConcurrencyException($"Hợp đồng đã được chỉnh sửa bởi phiên khác. Phiên bản hiện tại: {contract.Version}, phiên bản gửi lên: {expectedVersion}.");

        contract.ContractNo = request.ContractNo.Trim();
        contract.OwningOrganizationId = request.OwningOrganizationId;
        contract.VendorId = request.VendorId;
        contract.Status = request.Status.Trim();
        contract.SignedDate = request.SignedDate;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.TotalAmount = request.TotalAmount;
        contract.CurrencyCode = request.CurrencyCode.Trim().ToUpper();
        contract.MaintenanceStartDate = request.MaintenanceStartDate;
        contract.MaintenanceEndDate = request.MaintenanceEndDate;

        contract.Validate();

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetContractByIdAsync(contract.Id, cancellationToken)).Dto;
    }

    public async Task<ContractItemDto> AddContractItemAsync(Guid contractId, CreateContractItemRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException("Hợp đồng", contractId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.write",
                contract.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền thêm hạng mục cho hợp đồng này.");
        }

        var software = await _context.Software.FirstOrDefaultAsync(s => s.Id == request.SoftwareId, cancellationToken);
        if (software == null)
            throw new NotFoundException("Phần mềm", request.SoftwareId);

        var item = new ContractItem
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            SoftwareId = request.SoftwareId,
            Description = request.Description,
            Amount = request.Amount
        };

        item.Validate();

        _context.ContractItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return new ContractItemDto(
            item.Id,
            item.ContractId,
            item.SoftwareId,
            software.Name,
            item.Description,
            item.Amount,
            new List<LicenseEntitlementDto>());
    }

    public async Task DeleteContractItemAsync(Guid contractId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException("Hợp đồng", contractId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.write",
                contract.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền xóa hạng mục của hợp đồng này.");
        }

        var item = await _context.ContractItems.FirstOrDefaultAsync(ci => ci.Id == itemId && ci.ContractId == contractId, cancellationToken);
        if (item == null)
            throw new NotFoundException("Hạng mục hợp đồng", itemId);

        _context.ContractItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<LicenseEntitlementDto> AddEntitlementAsync(Guid contractId, Guid itemId, CreateLicenseEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException("Hợp đồng", contractId);

        if (_currentUser.UserId.HasValue)
        {
            var isAllowed = await _scopeAuth.HasPermissionAsync(
                _currentUser.UserId.Value,
                "contracts.write",
                contract.OwningOrganizationId,
                cancellationToken);

            if (!isAllowed)
                throw new ForbiddenException("Bạn không có quyền cấu hình hạn mức license cho hợp đồng này.");
        }

        var item = await _context.ContractItems.FirstOrDefaultAsync(ci => ci.Id == itemId && ci.ContractId == contractId, cancellationToken);
        if (item == null)
            throw new NotFoundException("Hạng mục hợp đồng", itemId);

        var entitlement = new LicenseEntitlement
        {
            Id = Guid.NewGuid(),
            ContractItemId = itemId,
            LicenseType = request.LicenseType.Trim(),
            Quantity = request.LicenseType.Equals("Seat", StringComparison.OrdinalIgnoreCase) ? request.Quantity : null,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        entitlement.Validate();

        _context.LicenseEntitlements.Add(entitlement);
        await _context.SaveChangesAsync(cancellationToken);

        return new LicenseEntitlementDto(
            entitlement.Id,
            entitlement.ContractItemId,
            entitlement.LicenseType,
            entitlement.Quantity,
            entitlement.ValidFrom,
            entitlement.ValidTo,
            0,
            entitlement.Quantity);
    }

    private static ContractDto MapToDto(Contract c, bool canReadFinancials)
    {
        var items = c.Items.Select(i =>
        {
            var entitlements = i.Entitlements.Select(e =>
            {
                var allocated = e.Allocations.Sum(a => a.Quantity);
                var remaining = e.Quantity.HasValue ? Math.Max(0, e.Quantity.Value - allocated) : (int?)null;
                return new LicenseEntitlementDto(
                    e.Id,
                    e.ContractItemId,
                    e.LicenseType,
                    e.Quantity,
                    e.ValidFrom,
                    e.ValidTo,
                    allocated,
                    remaining);
            }).ToList();

            return new ContractItemDto(
                i.Id,
                i.ContractId,
                i.SoftwareId,
                i.Software?.Name ?? string.Empty,
                i.Description,
                canReadFinancials ? i.Amount : null,
                entitlements);
        }).ToList();

        return new ContractDto(
            c.Id,
            c.ContractNo,
            c.OwningOrganizationId,
            c.OwningOrganization?.Versions?.FirstOrDefault(v => v.ValidTo == null)?.Name ?? c.OwningOrganization?.Code ?? string.Empty,
            c.VendorId,
            c.Vendor?.Name ?? string.Empty,
            c.Status,
            c.SignedDate,
            c.StartDate,
            c.EndDate,
            canReadFinancials ? c.TotalAmount : null,
            c.CurrencyCode,
            c.MaintenanceStartDate,
            c.MaintenanceEndDate,
            c.Version,
            items);
    }
}
