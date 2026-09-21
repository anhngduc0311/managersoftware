using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateVendorRequest(string Code, string Name, string? ContactInfo);
public record UpdateVendorRequest(string Name, string? ContactInfo, bool IsActive);

[Route("api/v1/vendors")]
public class VendorsController : BaseApiController
{
    private readonly IAppDbContext _context;

    public VendorsController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [RequirePermission("catalog.read")]
    public async Task<IActionResult> GetVendors()
    {
        var vendors = await _context.Vendors
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new
            {
                v.Id,
                v.Code,
                v.Name,
                v.ContactInfo,
                v.IsActive,
                v.CreatedAt
            })
            .ToListAsync();

        return Ok(vendors);
    }

    [HttpPost]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Mã và Tên nhà cung cấp không được để trống." });
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _context.Vendors.AnyAsync(v => v.Code == normalizedCode))
        {
            return Conflict(new { detail = $"Mã nhà cung cấp '{normalizedCode}' đã tồn tại." });
        }

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            ContactInfo = request.ContactInfo?.Trim(),
            IsActive = true
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        return Ok(vendor);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> UpdateVendor(Guid id, [FromBody] UpdateVendorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Tên nhà cung cấp không được để trống." });
        }

        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
        {
            return NotFound(new { detail = $"Không tìm thấy nhà cung cấp có ID: {id}" });
        }

        vendor.Name = request.Name.Trim();
        vendor.ContactInfo = request.ContactInfo?.Trim();
        vendor.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(vendor);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> DeleteVendor(Guid id)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
        {
            return NotFound(new { detail = $"Không tìm thấy nhà cung cấp có ID: {id}" });
        }

        var isUsed = await _context.Software.AnyAsync(s => s.VendorId == id);
        if (isUsed)
        {
            return BadRequest(new { detail = "Không thể xoá nhà cung cấp vì đang có phần mềm liên kết với đối tác này." });
        }

        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

