using LaoCai.SoftwareManagement.Api.Filters;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[RequirePermission("jobs.manage")]
public class JobsController : BaseApiController
{
    private readonly IBackgroundJobService _jobService;

    public JobsController(IBackgroundJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobService.GetJobsAsync(page, pageSize, status, type, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobByIdAsync(id, cancellationToken);
        return Ok(job);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> RetryJob(Guid id, CancellationToken cancellationToken)
    {
        await _jobService.RetryJobAsync(id, cancellationToken);
        return Ok(new { message = "Đã lên lịch chạy lại tác vụ." });
    }
}
