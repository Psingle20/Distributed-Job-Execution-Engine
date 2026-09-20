using Microsoft.Extensions.Logging;
using WorkerService.Application.Abstractions;

namespace WorkerService.Infrastructure.Chaos;

internal sealed class ChaosHook(
    IChaosState chaosState,
    ILogger<ChaosHook> logger) : IChaosHook
{
    public void Check(string checkpoint)
    {
        if (chaosState.IsActive(checkpoint))
        {
            logger.LogWarning("[CHAOS] {Checkpoint} triggered", checkpoint);
            throw new ChaosException($"{checkpoint}: simulated crash at checkpoint");
        }
    }

    public void CheckOnce(string checkpoint)
    {
        if (chaosState.IsActive(checkpoint))
        {
            chaosState.Deactivate(checkpoint);
            logger.LogWarning("[CHAOS] {Checkpoint} triggered (one-shot)", checkpoint);
            throw new ChaosException($"{checkpoint}: simulated failure at checkpoint");
        }
    }
}
