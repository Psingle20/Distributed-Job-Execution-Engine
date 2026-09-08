using CleanArchitecture.BuildingBlocks;

namespace WorkerService.Infrastructure;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
