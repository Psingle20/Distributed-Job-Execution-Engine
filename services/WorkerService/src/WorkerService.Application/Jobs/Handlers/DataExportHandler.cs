using System.Text.Json;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Handlers;

public sealed class DataExportHandler : IJobHandler
{
    public string JobType => "data-export";

    public async Task<string?> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        var export = new
        {
            context.JobId,
            context.AttemptNumber,
            ExportedAt = DateTimeOffset.UtcNow,
            RowCount = 1000,
            Format = "json"
        };

        return JsonSerializer.Serialize(export);
    }
}
