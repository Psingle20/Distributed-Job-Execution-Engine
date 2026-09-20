using CleanArchitecture.BuildingBlocks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using WorkerService.Application.Abstractions;
using WorkerService.Domain.Workers;

namespace WorkerService.Infrastructure.Services;

internal sealed class JobServiceClient(
    DaprClient daprClient,
    ILogger<JobServiceClient> logger) : IJobServiceClient
{
    private const string JobServiceAppId = "jobservice";

    public async Task<Result<ClaimResult>> ClaimJobAsync(Guid jobId, string workerId, CancellationToken cancellationToken)
    {
        try
        {
            ClaimResult claimResult = await daprClient.InvokeMethodAsync<object, ClaimResult>(
                HttpMethod.Post,
                JobServiceAppId,
                $"internal/jobs/{jobId}/claim",
                new { WorkerId = workerId },
                cancellationToken);

            return Result.Success(claimResult);
        }
        catch (InvocationException ex) when (ex.Response is not null && !ex.Response.IsSuccessStatusCode)
        {
            logger.LogWarning(ex, "Failed to claim job {JobId}: {StatusCode}", jobId, ex.Response.StatusCode);
            return Result.Failure<ClaimResult>(WorkerErrors.ClaimFailed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error claiming job {JobId}", jobId);
            return Result.Failure<ClaimResult>(WorkerErrors.ClaimFailed);
        }
    }

    public async Task<Result> HeartbeatAsync(Guid jobId, string workerId, Guid executionId, CancellationToken cancellationToken)
    {
        try
        {
            await daprClient.InvokeMethodAsync(
                JobServiceAppId,
                $"internal/jobs/{jobId}/heartbeat",
                new { WorkerId = workerId, ExecutionId = executionId },
                cancellationToken);

            return Result.Success();
        }
        catch (InvocationException ex) when (ex.Response is not null && !ex.Response.IsSuccessStatusCode)
        {
            logger.LogWarning(ex, "Heartbeat failed for job {JobId}: {StatusCode}", jobId, ex.Response.StatusCode);
            return Result.Failure(Error.Problem("Heartbeat.Failed", "Heartbeat request failed."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending heartbeat for job {JobId}", jobId);
            return Result.Failure(Error.Problem("Heartbeat.Error", ex.Message));
        }
    }
}
