using System.Collections.Concurrent;

namespace JobService.Infrastructure.Chaos;

internal sealed class InMemoryChaosState : IChaosState
{
    private readonly ConcurrentDictionary<string, ChaosPolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

    public void Activate(ChaosPolicy policy) => _policies[policy.Name] = policy;

    public void Deactivate(string policyName) => _policies.TryRemove(policyName, out _);

    public ChaosPolicy? Get(string policyName) =>
        _policies.TryGetValue(policyName, out ChaosPolicy? policy) ? policy : null;

    public bool IsActive(string policyName) => _policies.ContainsKey(policyName);

    public IReadOnlyList<ChaosPolicy> GetAll() => _policies.Values.ToList();
}
