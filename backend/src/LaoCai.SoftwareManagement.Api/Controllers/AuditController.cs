using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
public class AuditController : BaseApiController
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AuditController(
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var filter = new AuditFilterDto(
            page,
            pageSize,
            entityType,
            action,
            searchTerm,
            organizationId,
            fromDate,
            toDate,
            correlationId);

        var result = await _auditService.GetAuditLogsAsync(userId, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuditLogById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var log = await _auditService.GetAuditLogByIdAsync(id, userId, cancellationToken);
        if (log == null)
            return NotFound();

        return Ok(log);
    }
}
