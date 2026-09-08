using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WorkerService.Infrastructure.Chaos;

namespace WorkerService.Api.Controllers;

[Route("demo/failures")]
[ApiController]
[SuppressMessage("Sonar", "S6968", Justification = "Demo-only chaos endpoints return anonymous types")]
public sealed class ChaosController(IChaosState chaosState) : ControllerBase
{
    [HttpPost("crash-after-claim")]
    public IActionResult CrashAfterClaim()
    {
        chaosState.Activate(new ChaosPolicy(
            "crash-after-claim", ChaosType.CrashAfterClaim, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "crash-after-claim" });
    }

    [HttpPost("crash-after-effect")]
    public IActionResult CrashAfterEffect()
    {
        chaosState.Activate(new ChaosPolicy(
            "crash-after-effect", ChaosType.CrashAfterEffect, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "crash-after-effect" });
    }

    [HttpPost("fail-next-job")]
    public IActionResult FailNextJob()
    {
        chaosState.Activate(new ChaosPolicy(
            "fail-next-job", ChaosType.FailNextJob, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "fail-next-job" });
    }

    [HttpPost("fail-job-type")]
    public IActionResult FailJobType([FromQuery] string jobType, [FromQuery] int count = 3)
    {
        chaosState.Activate(new ChaosPolicy(
            "fail-job-type", ChaosType.FailJobType,
            new Dictionary<string, string>
            {
                ["jobType"] = jobType,
                ["count"] = count.ToString(CultureInfo.InvariantCulture)
            }));
        return Ok(new { Status = "activated", Policy = "fail-job-type", JobType = jobType, Count = count });
    }

    [HttpPost("delay-execution")]
    public IActionResult DelayExecution([FromQuery] int delayMs = 10000)
    {
        chaosState.Activate(new ChaosPolicy(
            "delay-execution", ChaosType.DelayExecution,
            new Dictionary<string, string> { ["delayMs"] = delayMs.ToString(CultureInfo.InvariantCulture) }));
        return Ok(new { Status = "activated", Policy = "delay-execution", DelayMs = delayMs });
    }

    [HttpPost("stop-heartbeat")]
    public IActionResult StopHeartbeat()
    {
        chaosState.Activate(new ChaosPolicy(
            "stop-heartbeat", ChaosType.StopHeartbeat, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "stop-heartbeat" });
    }

    [HttpPost("duplicate-event")]
    public IActionResult DuplicateEvent()
    {
        chaosState.Activate(new ChaosPolicy(
            "duplicate-event", ChaosType.DuplicateEvent, new Dictionary<string, string>()));
        return Ok(new { Status = "activated", Policy = "duplicate-event" });
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
