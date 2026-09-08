using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JobService.Infrastructure.Chaos;

internal sealed class ChaosEfInterceptor(
    IChaosState chaosState,
    ILogger<ChaosEfInterceptor> logger) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (chaosState.IsActive("crash-after-commit"))
        {
            logger.LogWarning("[CHAOS] crash-after-commit triggered — data is committed but caller will see exception");
            throw new ChaosException("crash-after-commit: simulated process crash after SaveChanges committed");
        }

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (chaosState.IsActive("crash-after-commit"))
        {
            logger.LogWarning("[CHAOS] crash-after-commit triggered — data is committed but caller will see exception");
            throw new ChaosException("crash-after-commit: simulated process crash after SaveChanges committed");
        }

        return result;
    }
}
