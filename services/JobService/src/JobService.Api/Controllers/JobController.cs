using CleanArchitecture.BuildingBlocks.Messaging;
using JobService.Application.Jobs.Dtos;
using JobService.Application.Jobs.Queries;
using JobService.Domain.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[Route("jobs")]
public sealed class JobController(IRequestHandler handler) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobDto dto,
        CancellationToken cancellationToken) =>
        MatchCreated(
            await handler.Handle<CreateJobDto, Guid>(dto, cancellationToken),
            nameof(GetById),
            null);

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var dto = new GetJobByIdDto { JobId = jobId };
        return Match(await handler.Handle<GetJobByIdDto, JobResponse>(dto, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] JobState? state,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = new SearchJobsDto { State = state, Type = type, Page = page, PageSize = pageSize };
        return Match(await handler.Handle<SearchJobsDto, IReadOnlyList<JobResponse>>(dto, cancellationToken));
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var dto = new CancelJobDto { JobId = jobId };
        return Match(await handler.Handle(dto, cancellationToken));
    }

    [HttpPost("{jobId:guid}/retry")]
    public async Task<IActionResult> Retry(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var dto = new RetryJobDto { JobId = jobId };
        return Match(await handler.Handle(dto, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var dto = new GetJobSummaryDto();
        return Match(await handler.Handle<GetJobSummaryDto, Dictionary<JobState, int>>(dto, cancellationToken));
    }
}
