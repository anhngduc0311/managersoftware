using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAntiforgery _antiforgery;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAntiforgery antiforgery, ICurrentUserService currentUserService)
    {
        _antiforgery = antiforgery;
        _currentUserService = currentUserService;
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        
        // Append non-HttpOnly cookie for Angular SPA to read
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken ?? "", new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        return Ok(new { token = tokens.RequestToken });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            id = _currentUserService.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
            userName = _currentUserService.UserName ?? "admin",
            displayName = "Quản trị viên Hệ thống",
            isAuthenticated = _currentUserService.IsAuthenticated,
            roles = new[] { "SystemAdmin" },
            permissions = new[]
            {
                "access.manage",
                "settings.manage",
                "jobs.manage",
                "organizations.read",
                "catalog.read"
            }
        });
    }
}
