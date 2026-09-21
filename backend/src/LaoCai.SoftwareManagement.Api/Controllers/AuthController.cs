using System.Security.Claims;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    private readonly IAntiforgery _antiforgery;
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IScopeAuthorizationService _scopeAuthService;

    public AuthController(
        IAntiforgery antiforgery,
        IAppDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasherService passwordHasher,
        IScopeAuthorizationService scopeAuthService)
    {
        _antiforgery = antiforgery;
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _scopeAuthService = scopeAuthService;
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken ?? "", new CookieOptions
        {
            HttpOnly = false,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Thông tin không hợp lệ",
                status = 400,
                detail = "Tên đăng nhập và mật khẩu không được để trống."
            });
        }

        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);

        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Đăng nhập thất bại",
                status = 401,
                detail = "Tên đăng nhập hoặc mật khẩu không chính xác."
            });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Tài khoản bị khóa",
                status = 401,
                detail = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên."
            });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("DisplayName", user.DisplayName),
            new("SecurityStamp", user.SecurityStamp)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        var grants = await _scopeAuthService.GetUserActiveGrantsAsync(user.Id);
        var permissions = grants.SelectMany(g => g.Permissions).Distinct().ToList();
        var roles = grants.Select(g => g.RoleCode).Distinct().ToList();

        return Ok(new
        {
            id = user.Id,
            username = user.UserName,
            displayName = user.DisplayName,
            email = user.Email,
            isAuthenticated = true,
            roles,
            permissions,
            scopes = grants
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("LaoCai_Session");
        Response.Cookies.Delete("XSRF-TOKEN");
        Response.Cookies.Delete("XSRF-TOKEN-COOKIE");
        return Ok(new { message = "Đăng xuất thành công" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Chưa đăng nhập",
                status = 401,
                isAuthenticated = false
            });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value);

        if (user == null || !user.IsActive)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Tài khoản không tồn tại hoặc đã bị khóa",
                status = 401,
                isAuthenticated = false
            });
        }

        var grants = await _scopeAuthService.GetUserActiveGrantsAsync(user.Id);
        var permissions = grants.SelectMany(g => g.Permissions).Distinct().ToList();
        var roles = grants.Select(g => g.RoleCode).Distinct().ToList();

        return Ok(new
        {
            id = user.Id,
            username = user.UserName,
            displayName = user.DisplayName,
            email = user.Email,
            isAuthenticated = true,
            roles,
            permissions,
            scopes = grants
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { detail = "Mật khẩu hiện tại và mật khẩu mới không được để trống." });
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new { detail = "Mật khẩu mới phải có tối thiểu 6 ký tự." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value);
        if (user == null)
        {
            return NotFound();
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            return BadRequest(new { detail = "Mật khẩu hiện tại không chính xác." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng sử dụng mật khẩu mới cho các phiên sau." });
    }
}
