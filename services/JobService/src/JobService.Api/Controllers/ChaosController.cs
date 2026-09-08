using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JobService.Infrastructure.Chaos;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[Route("demo/failures")]
[ApiController]
[SuppressMessage("Sonar", "S6968", Justification = "Demo-only chaos endpoints return anonymous types")]
public sealed class ChaosController(IChaosState chaosState) : ControllerBase
{
    [HttpPost("crash-after-commit")]
    public IActionResult CrashAfterCommit()
    {
        chaosState.Activate(new ChaosPolicy(
            "crash-after-commit", ChaosType.CrashAfterCommit, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "crash-after-commit" });
    }

    [HttpPost("pause-outbox")]
    public IActionResult PauseOutbox()
    {
        chaosState.Activate(new ChaosPolicy(
            "pause-outbox", ChaosType.PauseOutbox, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "pause-outbox" });
    }

    [HttpPost("resume-outbox")]
    public IActionResult ResumeOutbox()
    {
        chaosState.Deactivate("pause-outbox");
        return Ok(new { Status = "deactivated", Policy = "pause-outbox" });
    }

    [HttpPost("delay-outbox")]
    public IActionResult DelayOutbox([FromQuery] int delayMs = 5000)
    {
        chaosState.Activate(new ChaosPolicy(
            "delay-outbox", ChaosType.DelayOutbox,
            new Dictionary<string, string> { ["delayMs"] = delayMs.ToString(CultureInfo.InvariantCulture) }));
        return Ok(new { Status = "activated", Policy = "delay-outbox", DelayMs = delayMs });
    }

    [HttpPost("force-publish-failure")]
    public IActionResult ForcePublishFailure()
    {
        chaosState.Activate(new ChaosPolicy(
            "force-publish-failure", ChaosType.ForcePublishFailure, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "force-publish-failure" });
    }

    [HttpDelete("{policyName}")]
    public IActionResult Deactivate(string policyName)
    {
        chaosState.Deactivate(policyName);
        return Ok(new { Status = "deactivated", Policy = policyName });
    }

    [HttpGet]
    public IActionResult ListActive()
    {
        return Ok(chaosState.GetAll());
    }
}
