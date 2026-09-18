using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LaoCai.SoftwareManagement.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireIfMatchAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var ifMatch = context.HttpContext.Request.Headers["If-Match"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw new AppException("Yêu cầu cung cấp header 'If-Match' để chống ghi đè đồng thời.", "concurrency.if_match_required", StatusCodes.Status428PreconditionRequired);
        }

        base.OnActionExecuting(context);
    }
}

public class ETagResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value is IVersionedEntity versionedEntity)
        {
            context.HttpContext.Response.Headers["ETag"] = $"\"{versionedEntity.Version}\"";
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
