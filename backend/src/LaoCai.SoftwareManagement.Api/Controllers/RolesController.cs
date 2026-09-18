using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[Route("api/v1/roles")]
public class RolesController : BaseApiController
{
    private readonly IAppDbContext _context;

    public RolesController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                permissions = r.RolePermissions.Select(rp => new
                {
                    rp.Permission.Id,
                    rp.Permission.Code,
                    rp.Permission.Name,
                    rp.Permission.GroupName
                }).ToList()
            })
            .ToListAsync();

        return Ok(roles);
    }
}
