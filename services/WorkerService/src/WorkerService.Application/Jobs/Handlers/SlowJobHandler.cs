using System.Text.Json;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class SlowJobHandler : IJobHandler
{
    public string JobType => "slow-job";

    public async Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        int delaySeconds = 30;
        try
        {
            using var doc = JsonDocument.Parse(context.PayloadJson);
            if (doc.RootElement.TryGetProperty("delaySeconds", out JsonElement el))
            {
                delaySeconds = el.GetInt32();
            }
        }
        catch
        {
            // Use default
        }

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        return JsonSerializer.Serialize(new
        {
            context.JobId,
            DelaySeconds = delaySeconds,
            CompletedAt = DateTimeOffset.UtcNow
        });
    }
}
