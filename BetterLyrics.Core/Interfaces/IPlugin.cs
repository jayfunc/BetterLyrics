using BetterLyrics.Core.Interfaces.Infrastructure;

namespace BetterLyrics.Core.Interfaces
{
    public interface IPlugin : IAsyncDisposable
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Author { get; }
        string Version { get; }
        DateTime LastUpdated { get; }

        Task InitializeAsync(IPluginContext context);
    }
}
