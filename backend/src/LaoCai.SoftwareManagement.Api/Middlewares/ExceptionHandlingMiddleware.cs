using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Đã xảy ra lỗi hệ thống";
        var code = "system.internal_error";
        IDictionary<string, string[]>? errors = null;

        if (exception is AppException appEx)
        {
            statusCode = appEx.StatusCode;
            code = appEx.Code;
            title = appEx.Message;

            if (appEx is CustomValidationException valEx)
            {
                errors = valEx.Errors;
            }
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = "about:blank",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = correlationId;

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
