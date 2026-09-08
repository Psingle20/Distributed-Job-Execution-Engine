using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using Dapr;
using JobEngine.Contracts.IntegrationEvents;
using WorkerService.Application.Jobs.Commands.HandleJobCancelled;
using WorkerService.Application.Jobs.Commands.HandleJobCreated;
using WorkerService.Application.Jobs.Commands.HandleJobRetryScheduled;
using Microsoft.AspNetCore.Mvc;

namespace WorkerService.Api.Controllers;

[Route("dapr")]
[ApiController]
public sealed class DaprSubscriptionController(ICommandDispatcher commandDispatcher) : ControllerBase
{
    [HttpPost("jobs/created")]
    [Topic("pubsub", "jobs.created")]
    public async Task<IActionResult> HandleJobCreated(
        [FromBody] JobCreatedIntegrationEvent @event,
        [FromHeader(Name = "Traceparent")] string? traceparent,
        CancellationToken cancellationToken)
    {
        string messageId = traceparent ?? Guid.NewGuid().ToString();

        HandleJobCreatedCommand command = new(
            messageId,
            @event.JobId,
            @event.JobType,
            @event.Payload,
            @event.MaxAttempts,
            @event.CreatedAt);

        Result result = await commandDispatcher.Dispatch(command, cancellationToken);

        return result.IsSuccess ? Ok() : StatusCode(500);
    }

    [HttpPost("jobs/retry-scheduled")]
    [Topic("pubsub", "jobs.retry-scheduled")]
    public async Task<IActionResult> HandleJobRetryScheduled(
        [FromBody] JobRetryScheduledIntegrationEvent @event,
        [FromHeader(Name = "Traceparent")] string? traceparent,
        CancellationToken cancellationToken)
    {
        string messageId = traceparent ?? Guid.NewGuid().ToString();

        HandleJobRetryScheduledCommand command = new(
            messageId,
            @event.JobId,
            @event.JobType,
            @event.Payload,
            @event.AttemptNumber,
            @event.RetryAt);

        Result result = await commandDispatcher.Dispatch(command, cancellationToken);

        return result.IsSuccess ? Ok() : StatusCode(500);
    }

    [HttpPost("jobs/cancelled")]
    [Topic("pubsub", "jobs.cancelled")]
    public async Task<IActionResult> HandleJobCancelled(
        [FromBody] JobCancelledIntegrationEvent @event,
        [FromHeader(Name = "Traceparent")] string? traceparent,
        CancellationToken cancellationToken)
    {
        string messageId = traceparent ?? Guid.NewGuid().ToString();

        HandleJobCancelledCommand command = new(messageId, @event.JobId, @event.CancelledAt);

        Result result = await commandDispatcher.Dispatch(command, cancellationToken);

        return result.IsSuccess ? Ok() : StatusCode(500);
    }
}
