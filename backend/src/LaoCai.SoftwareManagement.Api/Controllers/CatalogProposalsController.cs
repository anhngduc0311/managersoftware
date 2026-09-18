using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateProposalRequest(
    Guid OrganizationId,
    string SoftwareName,
    string? Description);

public record AcceptProposalRequest(
    Guid? ExistingSoftwareId,
    string? NewSoftwareCode,
    Guid? CategoryId,
    Guid? VendorId);

public record RejectProposalRequest(string Reason);

[Route("api/v1/catalog-proposals")]
public class CatalogProposalsController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly ICatalogService _catalogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IScopeAuthorizationService _scopeAuthService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CatalogProposalsController(
        IAppDbContext context,
        ICatalogService catalogService,
        ICurrentUserService currentUserService,
        IScopeAuthorizationService scopeAuthService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _catalogService = catalogService;
        _currentUserService = currentUserService;
        _scopeAuthService = scopeAuthService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetProposals(
        [FromQuery] string? status,
        [FromQuery] Guid? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var userId = _currentUserService.UserId.Value;
        var canManageCatalog = await _scopeAuthService.HasPermissionAsync(userId, "catalog.manage");

        var query = _context.CatalogProposals
            .AsNoTracking()
            .Include(p => p.Organization)
            .Include(p => p.ProposedByUser)
            .Include(p => p.ReviewedByUser)
            .Include(p => p.CreatedSoftware)
            .AsQueryable();

        // If user is not CatalogManager, filter by their scope
        if (!canManageCatalog)
        {
            var allowedOrgs = await _scopeAuthService.GetAllowedOrganizationIdsAsync(userId, "catalog.propose");
            if (allowedOrgs != null)
            {
                query = query.Where(p => allowedOrgs.Contains(p.OrganizationId));
            }
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(p => p.OrganizationId == organizationId.Value);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.OrganizationId,
                OrganizationName = p.Organization.Code,
                p.ProposedByUserId,
                ProposedByUserName = p.ProposedByUser.DisplayName,
                p.SoftwareName,
                p.Description,
                p.Status,
                p.RejectionReason,
                p.CreatedSoftwareId,
                CreatedSoftwareName = p.CreatedSoftware != null ? p.CreatedSoftware.Name : null,
                p.ReviewedByUserId,
                ReviewedByUserName = p.ReviewedByUser != null ? p.ReviewedByUser.DisplayName : null,
                p.ReviewedAt,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            page,
            pageSize,
            totalCount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProposalById(Guid id)
    {
        var proposal = await _context.CatalogProposals
            .AsNoTracking()
            .Include(p => p.Organization)
            .Include(p => p.ProposedByUser)
            .Include(p => p.ReviewedByUser)
            .Include(p => p.CreatedSoftware)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proposal == null)
        {
            return NotFound(new { detail = $"Không tìm thấy đề xuất có ID: {id}" });
        }

        return Ok(new
        {
            proposal.Id,
            proposal.OrganizationId,
            OrganizationName = proposal.Organization.Code,
            proposal.ProposedByUserId,
            ProposedByUserName = proposal.ProposedByUser.DisplayName,
            proposal.SoftwareName,
            proposal.Description,
            proposal.Status,
            proposal.RejectionReason,
            proposal.CreatedSoftwareId,
            CreatedSoftwareName = proposal.CreatedSoftware?.Name,
            proposal.ReviewedByUserId,
            ReviewedByUserName = proposal.ReviewedByUser?.DisplayName,
            proposal.ReviewedAt,
            proposal.CreatedAt
        });
    }

    [HttpPost]
    [RequirePermission("catalog.propose")]
    public async Task<IActionResult> CreateProposal([FromBody] CreateProposalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SoftwareName))
        {
            return BadRequest(new { detail = "Tên phần mềm đề xuất không được để trống." });
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var hasOrgPermission = await _scopeAuthService.HasPermissionAsync(
            _currentUserService.UserId.Value,
            "catalog.propose",
            request.OrganizationId);

        if (!hasOrgPermission)
        {
            return StatusCode(403, new { detail = "Bạn không có quyền gửi đề xuất danh mục cho đơn vị này." });
        }

        var orgExists = await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId);
        if (!orgExists)
        {
            return NotFound(new { detail = "Đơn vị không tồn tại." });
        }

        var proposal = new CatalogProposal
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            ProposedByUserId = _currentUserService.UserId.Value,
            SoftwareName = request.SoftwareName.Trim(),
            Description = request.Description?.Trim(),
            Status = "Pending",
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.CatalogProposals.Add(proposal);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProposalById), new { id = proposal.Id }, proposal);
    }

    [HttpPost("{id:guid}/accept")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> AcceptProposal(Guid id, [FromBody] AcceptProposalRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var software = await _catalogService.AcceptProposalAsync(
                id,
                _currentUserService.UserId.Value,
                request.ExistingSoftwareId,
                request.NewSoftwareCode,
                request.CategoryId,
                request.VendorId);

            return Ok(new
            {
                message = "Đã phê duyệt đề xuất thành công.",
                softwareId = software.Id,
                softwareCode = software.Code,
                softwareName = software.Name
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> RejectProposal(Guid id, [FromBody] RejectProposalRequest request)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { detail = "Lý do từ chối đề xuất là bắt buộc." });
        }

        try
        {
            await _catalogService.RejectProposalAsync(
                id,
                _currentUserService.UserId.Value,
                request.Reason);

            return Ok(new { message = "Đã từ chối đề xuất." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }
}
