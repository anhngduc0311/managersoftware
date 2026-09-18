using System.Security.Claims;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LaoCai.SoftwareManagement.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    public string Permission { get; }
    public string? OrgIdParameterName { get; }

    public RequirePermissionAttribute(string permission, string? orgIdParameterName = null)
    {
        Permission = permission;
        OrgIdParameterName = orgIdParameterName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var currentUserService = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        var scopeAuthService = httpContext.RequestServices.GetRequiredService<IScopeAuthorizationService>();

        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            context.Result = new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Yêu cầu đăng nhập",
                status = StatusCodes.Status401Unauthorized,
                code = "auth.unauthenticated",
                traceId = currentUserService.CorrelationId
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        Guid? targetOrgId = null;
        if (!string.IsNullOrEmpty(OrgIdParameterName))
        {
            if (context.ActionArguments.TryGetValue(OrgIdParameterName, out var argVal))
            {
                if (argVal is Guid g)
                    targetOrgId = g;
                else if (argVal is string str && Guid.TryParse(str, out var parsedGuid))
                    targetOrgId = parsedGuid;
            }
            else if (httpContext.Request.Query.TryGetValue(OrgIdParameterName, out var queryVal) &&
                     Guid.TryParse(queryVal, out var queryGuid))
            {
                targetOrgId = queryGuid;
            }
        }

        var hasPermission = await scopeAuthService.HasPermissionAsync(
            currentUserService.UserId.Value,
            Permission,
            targetOrgId,
            httpContext.RequestAborted);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Từ chối truy cập",
                status = StatusCodes.Status403Forbidden,
                code = "auth.forbidden",
                detail = $"Bạn không có quyền '{Permission}' trong phạm vi yêu cầu.",
                traceId = currentUserService.CorrelationId
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
