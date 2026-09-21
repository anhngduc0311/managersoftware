using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/reminders")]
public class RemindersController : BaseApiController
{
    private readonly IExpirationReminderService _reminderService;

    public RemindersController(IExpirationReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessReminders(CancellationToken cancellationToken = default)
    {
        var result = await _reminderService.ProcessRemindersAsync(cancellationToken);
        return Ok(result);
    }
}
