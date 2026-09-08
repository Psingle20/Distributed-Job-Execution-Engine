namespace JobService.Infrastructure.Chaos;

public interface IChaosState
{
    void Activate(ChaosPolicy policy);
    void Deactivate(string policyName);
    ChaosPolicy? Get(string policyName);
    bool IsActive(string policyName);
    IReadOnlyList<ChaosPolicy> GetAll();
}
