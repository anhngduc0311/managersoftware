using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly? asOf,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? softwareId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? operationalStatus,
        CancellationToken cancellationToken = default)
    {
        var filter = new DashboardFilter(organizationId, softwareId, categoryId, operationalStatus, asOf);
        var result = await _dashboardService.GetDashboardKpisAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("coverage-eligibility")]
    public async Task<IActionResult> GetCoverageEligibilities(
        [FromQuery] Guid? softwareId,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetCoverageEligibilitiesAsync(softwareId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("coverage-eligibility")]
    public async Task<IActionResult> SetCoverageEligibility(
        [FromBody] CreateCoverageEligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.SetCoverageEligibilityAsync(request, cancellationToken);
        return Ok(result);
    }
}
