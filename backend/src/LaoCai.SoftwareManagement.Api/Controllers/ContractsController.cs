using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/contracts")]
public class ContractsController : BaseApiController
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    [RequirePermission("contracts.read", "contracts.write", "licenses.allocate", "deployments.read")]
    public async Task<IActionResult> GetContracts(
        [FromQuery] string? search,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? vendorId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ContractFilter(search, organizationId, vendorId, status, page, pageSize);
        var result = await _contractService.GetContractsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("contracts.read", "contracts.write", "licenses.allocate", "deployments.read")]
    public async Task<IActionResult> GetContractById(Guid id, CancellationToken cancellationToken)
    {
        var (dto, etag) = await _contractService.GetContractByIdAsync(id, cancellationToken);
        Response.Headers["ETag"] = etag;
        return Ok(dto);
    }

    [HttpPost]
    [RequirePermission("contracts.write")]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request, CancellationToken cancellationToken)
    {
        var result = await _contractService.CreateContractAsync(request, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return CreatedAtAction(nameof(GetContractById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("contracts.write")]
    [RequireIfMatch]
    public async Task<IActionResult> UpdateContract(Guid id, [FromBody] UpdateContractRequest request, CancellationToken cancellationToken)
    {
        var ifMatch = GetIfMatchVersion();
        var result = await _contractService.UpdateContractAsync(id, request, ifMatch, cancellationToken);
        Response.Headers["ETag"] = $"\"{result.Version}\"";
        return Ok(result);
    }

    [HttpPost("{id:guid}/items")]
    [RequirePermission("contracts.write")]
    public async Task<IActionResult> AddContractItem(Guid id, [FromBody] CreateContractItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _contractService.AddContractItemAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetContractById), new { id }, result);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [RequirePermission("contracts.write")]
    public async Task<IActionResult> DeleteContractItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await _contractService.DeleteContractItemAsync(id, itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/entitlements")]
    [RequirePermission("contracts.write")]
    public async Task<IActionResult> AddEntitlement(Guid id, Guid itemId, [FromBody] CreateLicenseEntitlementRequest request, CancellationToken cancellationToken)
    {
        var result = await _contractService.AddEntitlementAsync(id, itemId, request, cancellationToken);
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
