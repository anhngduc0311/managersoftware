using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateOrganizationRequest(
    string Code,
    string Name,
    Guid? ParentId,
    DateTime ValidFrom,
    DateTime? ValidTo);

public record UpdateOrganizationRequest(
    string Name,
    Guid? ParentId,
    DateTime ValidFrom,
    DateTime? ValidTo,
    bool IsActive);

public record CreateSuccessionRequest(
    Guid PredecessorId,
    Guid SuccessorId,
    DateTime EffectiveDate,
    string? Note);

[Route("api/v1/organizations")]
public class OrganizationsController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly IOrganizationService _organizationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OrganizationsController(
        IAppDbContext context,
        IOrganizationService organizationService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _organizationService = organizationService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    [RequirePermission("organizations.read")]
    public async Task<IActionResult> GetOrganizations(
        [FromQuery] string? search,
        [FromQuery] DateTime? asOf,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var effectiveDate = asOf ?? _dateTimeProvider.UtcNow;

        var query = _context.Organizations
            .AsNoTracking()
            .Include(o => o.Versions)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        var orgs = await query.ToListAsync();

        var mapped = orgs.Select(o =>
        {
            var version = o.Versions
                .Where(v => v.ValidFrom <= effectiveDate && (v.ValidTo == null || v.ValidTo > effectiveDate))
                .OrderByDescending(v => v.ValidFrom)
                .FirstOrDefault() ?? o.Versions.OrderByDescending(v => v.ValidFrom).FirstOrDefault();

            return new
            {
                o.Id,
                o.Code,
                o.IsActive,
                Name = version?.Name ?? o.Code,
                ParentId = version?.ParentId,
                ValidFrom = version?.ValidFrom ?? o.CreatedAt,
                ValidTo = version?.ValidTo,
                o.CreatedAt,
                o.UpdatedAt
            };
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            mapped = mapped.Where(m => m.Code.ToLower().Contains(term) || m.Name.ToLower().Contains(term));
        }

        var totalCount = mapped.Count();
        var items = mapped
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            items,
            page,
            pageSize,
            totalCount
        });
    }

    [HttpGet("tree")]
    [RequirePermission("organizations.read")]
    public async Task<IActionResult> GetOrganizationTree([FromQuery] DateTime? asOf)
    {
        var tree = await _organizationService.GetOrganizationTreeAsync(asOf);
        return Ok(tree);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("organizations.read")]
    public async Task<IActionResult> GetOrganizationById(Guid id, [FromQuery] DateTime? asOf)
    {
        var effectiveDate = asOf ?? _dateTimeProvider.UtcNow;
        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Versions)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
        {
            return NotFound(new { detail = $"Không tìm thấy cơ quan/đơn vị có ID: {id}" });
        }

        var version = org.Versions
            .Where(v => v.ValidFrom <= effectiveDate && (v.ValidTo == null || v.ValidTo > effectiveDate))
            .OrderByDescending(v => v.ValidFrom)
            .FirstOrDefault() ?? org.Versions.OrderByDescending(v => v.ValidFrom).FirstOrDefault();

        string? parentName = null;
        if (version?.ParentId.HasValue == true)
        {
            var parentVersion = await _context.OrganizationVersions
                .AsNoTracking()
                .Where(v => v.OrganizationId == version.ParentId.Value && v.ValidFrom <= effectiveDate && (v.ValidTo == null || v.ValidTo > effectiveDate))
                .FirstOrDefaultAsync();
            parentName = parentVersion?.Name;
        }

        return Ok(new
        {
            org.Id,
            org.Code,
            org.IsActive,
            Name = version?.Name ?? org.Code,
            ParentId = version?.ParentId,
            ParentName = parentName,
            ValidFrom = version?.ValidFrom ?? org.CreatedAt,
            ValidTo = version?.ValidTo,
            org.CreatedAt,
            org.UpdatedAt
        });
    }

    [HttpPost]
    [RequirePermission("organizations.manage")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Mã đơn vị và Tên cơ quan không được để trống." });
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _context.Organizations.AnyAsync(o => o.Code == normalizedCode))
        {
            return Conflict(new { detail = $"Mã đơn vị '{normalizedCode}' đã tồn tại trong hệ thống." });
        }

        if (request.ValidTo.HasValue && request.ValidTo.Value <= request.ValidFrom)
        {
            return BadRequest(new { detail = "Thời điểm kết thúc hiệu lực phải sau thời điểm bắt đầu." });
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Organizations.AnyAsync(o => o.Id == request.ParentId.Value);
            if (!parentExists)
            {
                return NotFound(new { detail = $"Không tìm thấy đơn vị cấp trên có ID: {request.ParentId.Value}" });
            }
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            IsActive = true,
            CreatedAt = nowUtc
        };

        var initialVersion = new OrganizationVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedAt = nowUtc
        };

        _context.Organizations.Add(org);
        _context.OrganizationVersions.Add(initialVersion);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganizationById), new { id = org.Id }, new
        {
            org.Id,
            org.Code,
            Name = initialVersion.Name,
            ParentId = initialVersion.ParentId,
            ValidFrom = initialVersion.ValidFrom,
            ValidTo = initialVersion.ValidTo,
            org.IsActive,
            org.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("organizations.manage")]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Tên cơ quan/đơn vị không được để trống." });
        }

        var org = await _context.Organizations
            .Include(o => o.Versions)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
        {
            return NotFound(new { detail = $"Không tìm thấy đơn vị có ID: {id}" });
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
            {
                return BadRequest(new { detail = "Đơn vị cấp trên không thể chính là bản thân đơn vị." });
            }

            var hasCycle = await _organizationService.HasCycleAsync(id, request.ParentId.Value, request.ValidFrom);
            if (hasCycle)
            {
                return BadRequest(new { detail = "Thiết lập quan hệ cấp trên gây ra chu trình vòng lặp phân cấp cây tổ chức." });
            }
        }

        if (request.ValidTo.HasValue && request.ValidTo.Value <= request.ValidFrom)
        {
            return BadRequest(new { detail = "Thời điểm kết thúc hiệu lực phải sau thời điểm bắt đầu." });
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        org.IsActive = request.IsActive;

        // Find current version or create a new version
        var currentVersion = org.Versions
            .Where(v => v.ValidFrom <= nowUtc && (v.ValidTo == null || v.ValidTo > nowUtc))
            .OrderByDescending(v => v.ValidFrom)
            .FirstOrDefault();

        if (currentVersion != null)
        {
            var hasOverlap = await _organizationService.HasDateOverlapAsync(id, request.ValidFrom, request.ValidTo, currentVersion.Id);
            if (hasOverlap)
            {
                return BadRequest(new { detail = "Khoảng thời gian hiệu lực bị chồng lấn với một phiên bản lịch sử khác của đơn vị." });
            }

            currentVersion.Name = request.Name.Trim();
            currentVersion.ParentId = request.ParentId;
            currentVersion.ValidFrom = request.ValidFrom;
            currentVersion.ValidTo = request.ValidTo;
            currentVersion.UpdatedAt = nowUtc;
        }
        else
        {
            var hasOverlap = await _organizationService.HasDateOverlapAsync(id, request.ValidFrom, request.ValidTo, null);
            if (hasOverlap)
            {
                return BadRequest(new { detail = "Khoảng thời gian hiệu lực bị chồng lấn với một phiên bản lịch sử khác của đơn vị." });
            }

            var newVersion = new OrganizationVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = request.Name.Trim(),
                ParentId = request.ParentId,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                CreatedAt = nowUtc
            };
            _context.OrganizationVersions.Add(newVersion);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            org.Id,
            org.Code,
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            org.IsActive,
            org.UpdatedAt
        });
    }

    [HttpGet("{id:guid}/history")]
    [RequirePermission("organizations.read")]
    public async Task<IActionResult> GetOrganizationHistory(Guid id)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Versions)
                .ThenInclude(v => v.Parent)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
        {
            return NotFound(new { detail = $"Không tìm thấy đơn vị có ID: {id}" });
        }

        var history = org.Versions
            .OrderByDescending(v => v.ValidFrom)
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.ParentId,
                v.ValidFrom,
                v.ValidTo,
                v.CreatedAt,
                v.UpdatedAt
            })
            .ToList();

        return Ok(history);
    }

    [HttpGet("successions")]
    [RequirePermission("organizations.read")]
    public async Task<IActionResult> GetSuccessions()
    {
        var successions = await _context.OrganizationSuccessions
            .AsNoTracking()
            .Include(s => s.Predecessor)
            .Include(s => s.Successor)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => new
            {
                s.Id,
                s.PredecessorId,
                PredecessorCode = s.Predecessor.Code,
                s.SuccessorId,
                SuccessorCode = s.Successor.Code,
                s.EffectiveDate,
                s.Note,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(successions);
    }

    [HttpPost("successions")]
    [RequirePermission("organizations.manage")]
    public async Task<IActionResult> CreateSuccession([FromBody] CreateSuccessionRequest request)
    {
        if (request.PredecessorId == request.SuccessorId)
        {
            return BadRequest(new { detail = "Đơn vị tiền nhiệm và đơn vị kế nhiệm không được trùng nhau." });
        }

        var pred = await _context.Organizations.AnyAsync(o => o.Id == request.PredecessorId);
        var succ = await _context.Organizations.AnyAsync(o => o.Id == request.SuccessorId);

        if (!pred || !succ)
        {
            return NotFound(new { detail = "Một trong hai đơn vị tiền nhiệm hoặc kế nhiệm không tồn tại." });
        }

        var succession = new OrganizationSuccession
        {
            Id = Guid.NewGuid(),
            PredecessorId = request.PredecessorId,
            SuccessorId = request.SuccessorId,
            EffectiveDate = request.EffectiveDate,
            Note = request.Note?.Trim(),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.OrganizationSuccessions.Add(succession);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            succession.Id,
            succession.PredecessorId,
            succession.SuccessorId,
            succession.EffectiveDate,
            succession.Note,
            succession.CreatedAt
        });
    }
}
