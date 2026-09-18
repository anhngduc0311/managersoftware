using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[Route("api/v1/permissions")]
public class PermissionsController : BaseApiController
{
    private readonly IAppDbContext _context;

    public PermissionsController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.Code)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.GroupName
            })
            .ToListAsync();

        return Ok(permissions);
    }
}
