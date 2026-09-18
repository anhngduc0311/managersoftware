using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateUserRoleScopeRequest(
    Guid UserId,
    Guid RoleId,
    string ScopeType, // "Global" | "Organization"
    Guid? OrganizationId,
    bool IncludeDescendants,
    DateTime ValidFrom,
    DateTime? ValidTo);

public record UpdateUserRoleScopeRequest(
    bool IncludeDescendants,
    DateTime ValidFrom,
    DateTime? ValidTo);

[Route("api/v1/user-role-scopes")]
[RequirePermission("access.manage")]
public class UserRoleScopesController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserRoleScopesController(
        IAppDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetGrants(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? roleId,
        [FromQuery] Guid? organizationId)
    {
        var query = _context.UserRoleScopes
            .AsNoTracking()
            .Include(urs => urs.User)
            .Include(urs => urs.Role)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(urs => urs.UserId == userId.Value);

        if (roleId.HasValue)
            query = query.Where(urs => urs.RoleId == roleId.Value);

        if (organizationId.HasValue)
            query = query.Where(urs => urs.OrganizationId == organizationId.Value);

        var grants = await query
            .OrderByDescending(urs => urs.ValidFrom)
            .Select(urs => new
            {
                urs.Id,
                urs.UserId,
                UserName = urs.User.UserName,
                UserDisplayName = urs.User.DisplayName,
                urs.RoleId,
                RoleCode = urs.Role.Code,
                RoleName = urs.Role.Name,
                urs.ScopeType,
                urs.OrganizationId,
                urs.IncludeDescendants,
                urs.ValidFrom,
                urs.ValidTo,
                IsActive = urs.ValidFrom <= _dateTimeProvider.UtcNow && (urs.ValidTo == null || urs.ValidTo > _dateTimeProvider.UtcNow)
            })
            .ToListAsync();

        return Ok(grants);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGrant([FromBody] CreateUserRoleScopeRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user == null)
        {
            return NotFound(new { detail = $"Không tìm thấy người dùng có ID: {request.UserId}" });
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role == null)
        {
            return NotFound(new { detail = $"Không tìm thấy vai trò có ID: {request.RoleId}" });
        }

        if (string.Equals(request.ScopeType, "Global", StringComparison.OrdinalIgnoreCase))
        {
            if (request.OrganizationId.HasValue)
            {
                return BadRequest(new { detail = "Phạm vi Toàn tỉnh (Global) không được gán mã đơn vị cụ thể." });
            }
        }
        else if (string.Equals(request.ScopeType, "Organization", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.OrganizationId.HasValue)
            {
                return BadRequest(new { detail = "Phạm vi Đơn vị (Organization) bắt buộc phải chọn đơn vị trực thuộc." });
            }

            var orgExists = await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId.Value);
            if (!orgExists)
            {
                return NotFound(new { detail = $"Không tìm thấy đơn vị có ID: {request.OrganizationId.Value}" });
            }
        }
        else
        {
            return BadRequest(new { detail = "Loại phạm vi không hợp lệ. Chỉ chấp nhận 'Global' hoặc 'Organization'." });
        }

        if (request.ValidTo.HasValue && request.ValidTo.Value <= request.ValidFrom)
        {
            return BadRequest(new { detail = "Thời điểm kết thúc hiệu lực phải sau thời điểm bắt đầu." });
        }

        var grant = new UserRoleScope
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            ScopeType = request.ScopeType,
            OrganizationId = request.OrganizationId,
            IncludeDescendants = request.IncludeDescendants,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        _context.UserRoleScopes.Add(grant);

        // Update SecurityStamp to invalidate existing cached session in <= 5 min
        user.SecurityStamp = Guid.NewGuid().ToString();

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGrants), new { userId = user.Id }, new
        {
            grant.Id,
            grant.UserId,
            grant.RoleId,
            grant.ScopeType,
            grant.OrganizationId,
            grant.IncludeDescendants,
            grant.ValidFrom,
            grant.ValidTo
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateGrant(Guid id, [FromBody] UpdateUserRoleScopeRequest request)
    {
        var grant = await _context.UserRoleScopes
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grant == null)
        {
            return NotFound(new { detail = $"Không tìm thấy bản cấp quyền có ID: {id}" });
        }

        if (request.ValidTo.HasValue && request.ValidTo.Value <= request.ValidFrom)
        {
            return BadRequest(new { detail = "Thời điểm kết thúc hiệu lực phải sau thời điểm bắt đầu." });
        }

        grant.IncludeDescendants = request.IncludeDescendants;
        grant.ValidFrom = request.ValidFrom;
        grant.ValidTo = request.ValidTo;

        grant.User.SecurityStamp = Guid.NewGuid().ToString();

        await _context.SaveChangesAsync();
        return Ok(new
        {
            grant.Id,
            grant.UserId,
            grant.RoleId,
            grant.ScopeType,
            grant.OrganizationId,
            grant.IncludeDescendants,
            grant.ValidFrom,
            grant.ValidTo
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeGrant(Guid id)
    {
        var grant = await _context.UserRoleScopes
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grant == null)
        {
            return NotFound(new { detail = $"Không tìm thấy bản cấp quyền có ID: {id}" });
        }

        _context.UserRoleScopes.Remove(grant);
        grant.User.SecurityStamp = Guid.NewGuid().ToString();

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
