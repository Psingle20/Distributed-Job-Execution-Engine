using Microsoft.AspNetCore.Mvc;

namespace WorkerService.Api.Controllers;

[Route("workers")]
[ApiController]
public sealed class WorkerStatusController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            WorkerId = $"worker-{Environment.MachineName}-{Environment.ProcessId}",
            Status = "Running",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
