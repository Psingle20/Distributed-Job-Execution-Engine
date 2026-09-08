using System.Globalization;
using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using WorkerService.Application.Jobs.Commands.HandleJobCreated;
using WorkerService.Application.Jobs.Commands.HandleJobRetryScheduled;

namespace WorkerService.Infrastructure.Chaos;

internal sealed class ChaosCommandDecorator<TCommand>(
    ICommandHandler<TCommand> inner,
    IChaosState chaosState,
    ILogger<ChaosCommandDecorator<TCommand>> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        string? jobType = GetJobType(command);

        await ApplyPreExecutionChaosAsync(jobType, cancellationToken);

        Result result = await inner.Handle(command, cancellationToken);

        ApplyPostExecutionChaos(jobType);

        return result;
    }

    private async Task ApplyPreExecutionChaosAsync(string? jobType, CancellationToken cancellationToken)
    {
        ChaosPolicy? delayPolicy = chaosState.Get("delay-execution");
        if (delayPolicy is not null
            && delayPolicy.Config.TryGetValue("delayMs", out string? delayStr)
            && int.TryParse(delayStr, CultureInfo.InvariantCulture, out int delayMs))
        {
            logger.LogWarning("[CHAOS] Delaying execution by {DelayMs}ms", delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }

        if (chaosState.IsActive("fail-next-job"))
        {
            chaosState.Deactivate("fail-next-job");
            logger.LogWarning("[CHAOS] fail-next-job triggered");
            throw new ChaosException("fail-next-job: simulated job handler failure");
        }

        ChaosPolicy? failTypePolicy = chaosState.Get("fail-job-type");
        if (failTypePolicy is not null
            && jobType is not null
            && failTypePolicy.Config.TryGetValue("jobType", out string? targetType)
            && string.Equals(targetType, jobType, StringComparison.OrdinalIgnoreCase)
            && failTypePolicy.Config.TryGetValue("count", out string? countStr)
            && int.TryParse(countStr, CultureInfo.InvariantCulture, out int remainingCount)
            && remainingCount > 0)
        {
            logger.LogWarning("[CHAOS] fail-job-type triggered for {JobType}, {Remaining} remaining",
                jobType, remainingCount - 1);

            if (remainingCount - 1 > 0)
            {
                chaosState.Activate(new ChaosPolicy(
                    "fail-job-type", ChaosType.FailJobType,
                    new Dictionary<string, string>
                    {
                        ["jobType"] = targetType,
                        ["count"] = (remainingCount - 1).ToString(CultureInfo.InvariantCulture)
                    }));
            }
            else
            {
                chaosState.Deactivate("fail-job-type");
            }

            throw new ChaosException($"fail-job-type: simulated failure for job type '{jobType}'");
        }
    }

    private void ApplyPostExecutionChaos(string? jobType)
    {
        if (jobType is null)
        {
            return;
        }

        if (chaosState.IsActive("crash-after-claim"))
        {
            logger.LogWarning("[CHAOS] crash-after-claim triggered");
            throw new ChaosException("crash-after-claim: simulated crash after successful claim and execution");
        }

        if (chaosState.IsActive("crash-after-effect"))
        {
            logger.LogWarning("[CHAOS] crash-after-effect triggered");
            throw new ChaosException("crash-after-effect: simulated crash after side effect completed");
        }
    }

    private static string? GetJobType(TCommand command)
    {
        return command switch
        {
            HandleJobCreatedCommand c => c.JobType,
            HandleJobRetryScheduledCommand c => c.JobType,
            _ => null
        };
    }
}
