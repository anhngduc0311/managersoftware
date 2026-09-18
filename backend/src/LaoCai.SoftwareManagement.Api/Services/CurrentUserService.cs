using System.Security.Claims;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;

namespace LaoCai.SoftwareManagement.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? CorrelationId
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Items.TryGetValue("CorrelationId", out var val) == true)
            {
                return val?.ToString();
            }
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
