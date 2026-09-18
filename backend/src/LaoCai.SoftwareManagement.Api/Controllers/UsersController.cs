using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record CreateUserRequest(string Username, string DisplayName, string? Email, string Password);
public record UpdateUserRequest(string DisplayName, string? Email, bool IsActive);
public record AdminResetPasswordRequest(string NewPassword);

[Route("api/v1/users")]
[RequirePermission("access.manage")]
public class UsersController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IScopeAuthorizationService _scopeAuthService;

    public UsersController(
        IAppDbContext context,
        IPasswordHasherService passwordHasher,
        IScopeAuthorizationService scopeAuthService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _scopeAuthService = scopeAuthService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.UserName.ToLower().Contains(term) || u.DisplayName.ToLower().Contains(term) || (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.DisplayName,
                u.Email,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items = users,
            page,
            pageSize,
            totalCount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { detail = $"Không tìm thấy người dùng có ID: {id}" });
        }

        var grants = await _scopeAuthService.GetUserActiveGrantsAsync(user.Id);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            scopes = grants
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { detail = "Tên đăng nhập, họ tên và mật khẩu không được để trống." });
        }

        var normalized = request.Username.Trim().ToUpperInvariant();
        if (await _context.Users.AnyAsync(u => u.NormalizedUserName == normalized))
        {
            return Conflict(new { detail = $"Tên đăng nhập '{request.Username}' đã tồn tại trong hệ thống." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Username.Trim(),
            NormalizedUserName = normalized,
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email?.Trim(),
            NormalizedEmail = request.Email?.Trim().ToUpperInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
        {
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { detail = $"Không tìm thấy người dùng có ID: {id}" });
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email?.Trim();
        user.NormalizedEmail = request.Email?.Trim().ToUpperInvariant();

        if (user.IsActive != request.IsActive)
        {
            user.IsActive = request.IsActive;
            // Invalidate existing sessions on lock
            user.SecurityStamp = Guid.NewGuid().ToString();
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.UpdatedAt
        });
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { detail = "Mật khẩu mới phải có tối thiểu 6 ký tự." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { detail = $"Không tìm thấy người dùng có ID: {id}" });
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã đặt lại mật khẩu cho tài khoản '{user.UserName}' thành công." });
    }

    [HttpPost("{id:guid}/toggle-lock")]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { detail = $"Không tìm thấy người dùng có ID: {id}" });
        }

        user.IsActive = !user.IsActive;
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _context.SaveChangesAsync();

        var statusText = user.IsActive ? "Mở khóa" : "Khóa";
        return Ok(new
        {
            user.Id,
            user.IsActive,
            message = $"Đã {statusText} tài khoản '{user.UserName}' thành công."
        });
    }
}
