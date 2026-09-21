using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/deployments")]
public class DeploymentsController : BaseApiController
{
    private readonly IDeploymentService _deploymentService;
    private readonly ICurrentUserService _currentUserService;

    public DeploymentsController(
        IDeploymentService deploymentService,
        ICurrentUserService currentUserService)
    {
        _deploymentService = deploymentService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDeployments(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? softwareId,
        [FromQuery] string? environment,
        [FromQuery] string? operationalStatus,
        [FromQuery] string? workflowStatus,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var filter = new DeploymentFilterDto(
            organizationId,
            softwareId,
            environment,
            operationalStatus,
            workflowStatus,
            search,
            page,
            pageSize);

        var result = await _deploymentService.GetDeploymentsAsync(filter, currentUserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeploymentById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _deploymentService.GetDeploymentByIdAsync(id, currentUserId, cancellationToken);

        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("deployments.write")]
    public async Task<IActionResult> CreateDeployment([FromBody] CreateDeploymentDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _deploymentService.CreateDeploymentAsync(dto, currentUserId, cancellationToken);

        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return CreatedAtAction(nameof(GetDeploymentById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/revisions")]
    [RequirePermission("deployments.write")]
    [RequireIfMatch]
    public async Task<IActionResult> CreateNextRevision(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.CreateNextRevisionAsync(id, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return CreatedAtAction(
            actionName: "GetRevisionById",
            controllerName: "DeploymentRevisions",
            routeValues: new { id = result.Id },
            value: result);
    }

    private long GetIfMatchVersion()
    {
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw new AppException("Yêu cầu header 'If-Match' để kiểm soát phiên bản.", "concurrency.if_match_required", StatusCodes.Status428PreconditionRequired);
        }

        var cleanVal = ifMatch.Trim('\"', ' ');
        if (!long.TryParse(cleanVal, out var version))
        {
            throw new AppException("Header 'If-Match' không đúng định dạng số nguyên.", "concurrency.invalid_if_match", StatusCodes.Status400BadRequest);
        }

        return version;
    }
}
