namespace WorkerService.Application.Abstractions;

public interface IChaosHook
{
    void Check(string checkpoint);
    void CheckOnce(string checkpoint);
}
