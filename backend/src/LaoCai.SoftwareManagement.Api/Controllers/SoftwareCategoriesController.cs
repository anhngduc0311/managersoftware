using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateCategoryRequest(string Code, string Name);
public record UpdateCategoryRequest(string Name, bool IsActive);

[Route("api/v1/software-categories")]
public class SoftwareCategoriesController : BaseApiController
{
    private readonly IAppDbContext _context;

    public SoftwareCategoriesController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [RequirePermission("catalog.read")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.SoftwareCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.IsActive,
                c.CreatedAt
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Mã và Tên nhóm phần mềm không được để trống." });
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _context.SoftwareCategories.AnyAsync(c => c.Code == normalizedCode))
        {
            return Conflict(new { detail = $"Mã nhóm phần mềm '{normalizedCode}' đã tồn tại." });
        }

        var category = new SoftwareCategory
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            IsActive = true
        };

        _context.SoftwareCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("catalog.manage")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { detail = "Tên nhóm phần mềm không được để trống." });
        }

        var category = await _context.SoftwareCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return NotFound(new { detail = $"Không tìm thấy nhóm phần mềm có ID: {id}" });
        }

        category.Name = request.Name.Trim();
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(category);
    }
}
