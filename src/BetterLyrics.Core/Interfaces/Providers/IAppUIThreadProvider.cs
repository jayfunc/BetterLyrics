namespace BetterLyrics.Core.Interfaces.Providers;

public interface IAppUIThreadProvider
{
    void Initialize(object? obj);
    void Execute(Action action);
    Task RunAsync(Action action);
}