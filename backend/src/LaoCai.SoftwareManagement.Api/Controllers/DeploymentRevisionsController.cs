using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/deployment-revisions")]
public class DeploymentRevisionsController : BaseApiController
{
    private readonly IDeploymentService _deploymentService;
    private readonly ICurrentUserService _currentUserService;

    public DeploymentRevisionsController(
        IDeploymentService deploymentService,
        ICurrentUserService currentUserService)
    {
        _deploymentService = deploymentService;
        _currentUserService = currentUserService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRevisionById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _deploymentService.GetRevisionByIdAsync(id, currentUserId, cancellationToken);

        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpGet("{id:guid}/decisions")]
    public async Task<IActionResult> GetRevisionDecisions(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var revision = await _deploymentService.GetRevisionByIdAsync(id, currentUserId, cancellationToken);
        return Ok(revision.Decisions ?? new List<ApprovalDecisionDto>());
    }

    [HttpGet("history/{deploymentId:guid}")]
    public async Task<IActionResult> GetRevisionHistory(Guid deploymentId, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var history = await _deploymentService.GetRevisionHistoryAsync(deploymentId, currentUserId, cancellationToken);
        return Ok(history);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("deployments.write")]
    [RequireIfMatch]
    public async Task<IActionResult> UpdateDraftRevision(
        Guid id,
        [FromBody] UpdateDraftRevisionDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.UpdateDraftRevisionAsync(id, dto, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission("deployments.write")]
    [RequireIfMatch]
    public async Task<IActionResult> SubmitRevision(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.SubmitRevisionAsync(id, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("deployments.approve")]
    [RequireIfMatch]
    public async Task<IActionResult> ApproveRevision(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.ApproveRevisionAsync(id, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission("deployments.approve")]
    [RequireIfMatch]
    public async Task<IActionResult> RejectRevision(
        Guid id,
        [FromBody] RejectRevisionDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.RejectRevisionAsync(id, dto, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/reopen")]
    [RequirePermission("deployments.write")]
    [RequireIfMatch]
    public async Task<IActionResult> ReopenRevision(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var ifMatch = GetIfMatchVersion();

        var result = await _deploymentService.ReopenRejectedRevisionAsync(id, ifMatch, currentUserId, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
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
