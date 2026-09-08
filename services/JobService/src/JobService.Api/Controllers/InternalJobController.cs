using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[Route("internal/jobs")]
public sealed class InternalJobController(IRequestHandler handler) : ApiController
{
    [HttpPost("{jobId:guid}/claim")]
    public async Task<IActionResult> Claim(
        [FromRoute] Guid jobId,
        [FromBody] ClaimJobDto dto,
        CancellationToken cancellationToken)
    {
        dto.JobId = jobId;
        return Match(await handler.Handle(dto, cancellationToken));
    }

    [HttpPost("{jobId:guid}/heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromRoute] Guid jobId,
        [FromBody] HeartbeatJobDto dto,
        CancellationToken cancellationToken)
    {
        dto.JobId = jobId;
        return Match(await handler.Handle(dto, cancellationToken));
    }
}
