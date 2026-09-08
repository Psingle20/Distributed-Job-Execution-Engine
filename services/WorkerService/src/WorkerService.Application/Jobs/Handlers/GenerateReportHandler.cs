using System.Text.Json;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class GenerateReportHandler : IJobHandler
{
    public string JobType => "generate-report";

    public async Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        var report = new
        {
            context.JobId,
            context.AttemptNumber,
            GeneratedAt = DateTimeOffset.UtcNow,
            Status = "completed",
            Summary = $"Report generated for payload: {context.PayloadJson}"
        };

        return JsonSerializer.Serialize(report);
    }
}
