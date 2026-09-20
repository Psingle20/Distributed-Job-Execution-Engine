using System.Text.Json;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class FailNTimesThenSucceedHandler : IJobHandler
{
    public string JobType => "fail-n-times";

    public Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        int failCount = 3;
        try
        {
            using var doc = JsonDocument.Parse(context.PayloadJson);
            if (doc.RootElement.TryGetProperty("failCount", out JsonElement el))
            {
                failCount = el.GetInt32();
            }
        }
        catch (JsonException)
        {
            // Invalid payload — use default failCount
        }

        if (context.AttemptNumber <= failCount)
        {
            throw new InvalidOperationException(
                $"FailNTimesThenSucceed: deliberate failure on attempt {context.AttemptNumber} of {failCount} for job {context.JobId}");
        }

        string result = JsonSerializer.Serialize(new
        {
            context.JobId,
            SucceededAfterFailures = failCount,
            context.AttemptNumber
        });

        return Task.FromResult<string?>(result);
    }
}
