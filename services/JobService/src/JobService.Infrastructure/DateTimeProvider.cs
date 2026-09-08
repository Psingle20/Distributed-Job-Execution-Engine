using CleanArchitecture.BuildingBlocks;

namespace JobService.Infrastructure;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
