using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateSoftwareRequest(
    string Code,
    string Name,
    Guid CategoryId,
    Guid VendorId,
    string? Description,
    string LifecycleStatus);

public record UpdateSoftwareRequest(
    string Name,
    Guid CategoryId,
    Guid VendorId,
    string? Description,
    string LifecycleStatus);

public record CreateReleaseRequest(
    string VersionName,
    DateTime ReleaseDate,
    DateTime? SupportEndDate);

public record UpdateReleaseRequest(
    DateTime ReleaseDate,
    DateTime? SupportEndDate);

[Route("api/v1/software")]
public class SoftwareController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly ICatalogService _catalogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SoftwareController(
        IAppDbContext context,
        ICatalogService catalogService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _catalogService = catalogService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    [RequirePermission("catalog.read")]
    public async Task<IActionResult> GetSoftware(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? vendorId,
        [FromQuery] string? lifecycleStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Software
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.Vendor)
            .Include(s => s.Releases)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(term) || s.Name.ToLower().Contains(term));
        }

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (vendorId.HasValue)
            query = query.Where(s => s.VendorId == vendorId.Value);

        if (!string.IsNullOrWhiteSpace(lifecycleStatus))
            query = query.Where(s => s.LifecycleStatus == lifecycleStatus);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Code,
                s.Name,
                s.CategoryId,
                CategoryName = s.Category.Name,
                s.VendorId,
                VendorName = s.Vendor.Name,
                s.Description,
                s.LifecycleStatus,
                s.Version,
                ReleaseCount = s.Releases.Count,
                LatestRelease = s.Releases.OrderByDescending(r => r.ReleaseDate).Select(r => r.VersionName).FirstOrDefault(),
                s.CreatedAt,
                s.UpdatedAt
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
    [RequirePermission("catalog.read")]
    public async Task<IActionResult> GetSoftwareById(Guid id)
    {
        var software = await _context.Software
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.Vendor)
            .Include(s => s.Releases)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (software == null)
        {
            return NotFound(new { detail = $"Không tìm thấy phần mềm có ID: {id}" });
        }

        return Ok(new
        {
            software.Id,
            software.Code,
            software.Name,
            software.CategoryId,
            CategoryName = software.Category.Name,
            software.VendorId,
            VendorName = software.Vendor.Name,
            software.Description,
            software.LifecycleStatus,
            software.Version,
            Releases = software.Releases.OrderByDescending(r => r.ReleaseDate).Select(r => new
            {
                r.Id,
                r.VersionName,
                r.ReleaseDate,
                r.SupportEndDate,
                r.CreatedAt
            }).ToList(),
            software.CreatedAt,
            software.UpdatedAt
        });
    }

    [HttpPost]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> CreateSoftware([FromBody] CreateSoftwareRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Mã và Tên phần mềm không được để trống." });
        }

        var isUnique = await _catalogService.IsSoftwareCodeUniqueAsync(request.Code);
        if (!isUnique)
        {
            return Conflict(new { detail = $"Mã phần mềm '{request.Code}' đã tồn tại trong hệ thống." });
        }

        var catExists = await _context.SoftwareCategories.AnyAsync(c => c.Id == request.CategoryId);
        var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == request.VendorId);

        if (!catExists || !vendorExists)
        {
            return NotFound(new { detail = "Nhóm phần mềm hoặc Nhà cung cấp không tồn tại." });
        }

        var software = new Software
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            VendorId = request.VendorId,
            Description = request.Description?.Trim(),
            LifecycleStatus = string.IsNullOrWhiteSpace(request.LifecycleStatus) ? "Active" : request.LifecycleStatus,
            Version = 1,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.Software.Add(software);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSoftwareById), new { id = software.Id }, software);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> UpdateSoftware(Guid id, [FromBody] UpdateSoftwareRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Tên phần mềm không được để trống." });
        }

        var software = await _context.Software.FirstOrDefaultAsync(s => s.Id == id);
        if (software == null)
        {
            return NotFound(new { detail = $"Không tìm thấy phần mềm có ID: {id}" });
        }

        var catExists = await _context.SoftwareCategories.AnyAsync(c => c.Id == request.CategoryId);
        var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == request.VendorId);

        if (!catExists || !vendorExists)
        {
            return NotFound(new { detail = "Nhóm phần mềm hoặc Nhà cung cấp không tồn tại." });
        }

        software.Name = request.Name.Trim();
        software.CategoryId = request.CategoryId;
        software.VendorId = request.VendorId;
        software.Description = request.Description?.Trim();
        software.LifecycleStatus = request.LifecycleStatus;

        await _context.SaveChangesAsync();
        return Ok(software);
    }

    [HttpGet("{id:guid}/releases")]
    [RequirePermission("catalog.read")]
    public async Task<IActionResult> GetReleases(Guid id)
    {
        var releases = await _context.SoftwareReleases
            .AsNoTracking()
            .Where(r => r.SoftwareId == id)
            .OrderByDescending(r => r.ReleaseDate)
            .Select(r => new
            {
                r.Id,
                r.SoftwareId,
                r.VersionName,
                r.ReleaseDate,
                r.SupportEndDate,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(releases);
    }

    [HttpPost("{id:guid}/releases")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> CreateRelease(Guid id, [FromBody] CreateReleaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VersionName))
        {
            return BadRequest(new { detail = "Tên phiên bản phát hành không được để trống." });
        }

        var software = await _context.Software.FirstOrDefaultAsync(s => s.Id == id);
        if (software == null)
        {
            return NotFound(new { detail = $"Không tìm thấy phần mềm có ID: {id}" });
        }

        var isUnique = await _catalogService.IsReleaseVersionUniqueAsync(id, request.VersionName);
        if (!isUnique)
        {
            return Conflict(new { detail = $"Phiên bản '{request.VersionName}' đã tồn tại cho phần mềm này." });
        }

        var release = new SoftwareRelease
        {
            Id = Guid.NewGuid(),
            SoftwareId = id,
            VersionName = request.VersionName.Trim(),
            ReleaseDate = request.ReleaseDate,
            SupportEndDate = request.SupportEndDate,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.SoftwareReleases.Add(release);
        software.Version += 1;

        await _context.SaveChangesAsync();
        return Ok(release);
    }

    [HttpPut("/api/v1/software-releases/{id:guid}")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> UpdateRelease(Guid id, [FromBody] UpdateReleaseRequest request)
    {
        var release = await _context.SoftwareReleases
            .Include(r => r.Software)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (release == null)
        {
            return NotFound(new { detail = $"Không tìm thấy phiên bản phát hành có ID: {id}" });
        }

        release.ReleaseDate = request.ReleaseDate;
        release.SupportEndDate = request.SupportEndDate;
        release.Software.Version += 1;

        await _context.SaveChangesAsync();
        return Ok(release);
    }
}
