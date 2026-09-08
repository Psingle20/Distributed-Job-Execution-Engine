using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using Dapr;
using JobEngine.Contracts.IntegrationEvents;
using JobService.Application.Jobs.Commands.HandleJobExecutionCompleted;
using JobService.Application.Jobs.Commands.HandleJobExecutionFailed;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Api.Controllers;

[Route("dapr")]
[ApiController]
public sealed class DaprSubscriptionController(ICommandDispatcher commandDispatcher) : ControllerBase
{
    [HttpPost("job-execution-completed")]
    [Topic("pubsub", "job-executions.completed")]
    public async Task<IActionResult> HandleExecutionCompleted(
        [FromBody] JobExecutionCompletedIntegrationEvent @event,
        [FromHeader(Name = "Traceparent")] string? traceparent,
        CancellationToken cancellationToken)
    {
        string messageId = traceparent ?? Guid.NewGuid().ToString();

        HandleJobExecutionCompletedCommand command = new(
            messageId,
            @event.JobId,
            @event.WorkerId,
            @event.AttemptNumber,
            @event.ResultPayload,
            @event.CompletedAt);

        Result result = await commandDispatcher.Dispatch(command, cancellationToken);

        return result.IsSuccess ? Ok() : StatusCode(500);
    }

    [HttpPost("job-execution-failed")]
    [Topic("pubsub", "job-executions.failed")]
    public async Task<IActionResult> HandleExecutionFailed(
        [FromBody] JobExecutionFailedIntegrationEvent @event,
        [FromHeader(Name = "Traceparent")] string? traceparent,
        CancellationToken cancellationToken)
    {
        string messageId = traceparent ?? Guid.NewGuid().ToString();

        HandleJobExecutionFailedCommand command = new(
            messageId,
            @event.JobId,
            @event.WorkerId,
            @event.AttemptNumber,
            @event.ErrorMessage,
            @event.IsRetryable,
            @event.FailedAt);

        Result result = await commandDispatcher.Dispatch(command, cancellationToken);

        return result.IsSuccess ? Ok() : StatusCode(500);
    }
}
