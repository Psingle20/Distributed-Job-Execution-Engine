using CleanArchitecture.BuildingBlocks;

namespace JobService.Application.Tests.TestHelpers;

internal sealed class MockDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
