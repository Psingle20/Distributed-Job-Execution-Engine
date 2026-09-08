namespace JobService.Infrastructure.Chaos;

public sealed class ChaosException(string message) : Exception(message);
