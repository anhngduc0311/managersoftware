using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/license-allocations")]
public class LicenseAllocationsController : BaseApiController
{
    private readonly ILicenseAllocationService _allocationService;

    public LicenseAllocationsController(ILicenseAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet("entitlement/{entitlementId:guid}")]
    public async Task<IActionResult> GetAllocationsByEntitlement(Guid entitlementId, CancellationToken cancellationToken)
    {
        var result = await _allocationService.GetAllocationsByEntitlementAsync(entitlementId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("deployment/{deploymentId:guid}")]
    public async Task<IActionResult> GetAllocationsByDeployment(Guid deploymentId, CancellationToken cancellationToken)
    {
        var result = await _allocationService.GetAllocationsByDeploymentAsync(deploymentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("licenses.allocate")]
    public async Task<IActionResult> AllocateLicense([FromBody] CreateAllocationRequest request, CancellationToken cancellationToken)
    {
        var result = await _allocationService.AllocateLicenseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAllocationsByEntitlement), new { entitlementId = result.EntitlementId }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("licenses.allocate")]
    public async Task<IActionResult> UpdateAllocation(Guid id, [FromBody] UpdateAllocationRequest request, CancellationToken cancellationToken)
    {
        var result = await _allocationService.UpdateAllocationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("licenses.allocate")]
    public async Task<IActionResult> RevokeAllocation(Guid id, CancellationToken cancellationToken)
    {
        await _allocationService.RevokeAllocationAsync(id, cancellationToken);
        return NoContent();
    }
}
