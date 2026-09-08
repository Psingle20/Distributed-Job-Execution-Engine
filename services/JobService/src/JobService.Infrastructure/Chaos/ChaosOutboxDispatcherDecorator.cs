using System.Globalization;
using Dapr.EntityFrameworkCore.Outbox;
using Microsoft.Extensions.Logging;

namespace JobService.Infrastructure.Chaos;

internal sealed class ChaosOutboxDispatcherDecorator(
    IOutboxDispatcher inner,
    IChaosState chaosState,
    ILogger<ChaosOutboxDispatcherDecorator> logger) : IOutboxDispatcher
{
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        if (chaosState.IsActive("pause-outbox"))
        {
            logger.LogWarning("[CHAOS] Outbox dispatch paused");
            return 0;
        }

        ChaosPolicy? delayPolicy = chaosState.Get("delay-outbox");
        if (delayPolicy is not null
            && delayPolicy.Config.TryGetValue("delayMs", out string? delayStr)
            && int.TryParse(delayStr, CultureInfo.InvariantCulture, out int delayMs))
        {
            logger.LogWarning("[CHAOS] Delaying outbox dispatch by {DelayMs}ms", delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }

        if (chaosState.IsActive("force-publish-failure"))
        {
            logger.LogWarning("[CHAOS] Forcing outbox publish failure");
            throw new ChaosException("force-publish-failure: simulated outbox dispatch failure");
        }

        return await inner.DispatchPendingAsync(cancellationToken);
    }
}
