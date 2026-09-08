using System.Collections.Concurrent;
using System.Text.Json;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class FailNTimesThenSucceedHandler : IJobHandler
{
    public string JobType => "fail-n-times";

    private static readonly ConcurrentDictionary<Guid, int> FailureCounts = new();

    public Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        int failAfter = 3;
        try
        {
            using var doc = JsonDocument.Parse(context.PayloadJson);
            if (doc.RootElement.TryGetProperty("failCount", out JsonElement el))
            {
                failAfter = el.GetInt32();
            }
        }
        catch
        {
            // Use default
        }

        int failuresSoFar = FailureCounts.AddOrUpdate(context.JobId, 1, (_, c) => c + 1);

        if (failuresSoFar <= failAfter)
        {
            throw new InvalidOperationException(
                $"FailNTimesThenSucceed: deliberate failure #{failuresSoFar} of {failAfter} for job {context.JobId}");
        }

        FailureCounts.TryRemove(context.JobId, out _);

        string result = JsonSerializer.Serialize(new
        {
            context.JobId,
            SucceededAfterFailures = failAfter,
            context.AttemptNumber
        });

        return Task.FromResult<string?>(result);
    }
}
