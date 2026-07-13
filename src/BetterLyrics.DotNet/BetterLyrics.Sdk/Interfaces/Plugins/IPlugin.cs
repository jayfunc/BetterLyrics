using BetterLyrics.Sdk.Models.SettingsSchema;

namespace BetterLyrics.Sdk.Interfaces.Plugins;

public interface IPlugin : IAsyncDisposable
{
    string Title { get; }
    string Description { get; }
    string Author { get; }

    string Id { get; }
    string Version { get; }
    DateTime LastUpdated { get; }

    string RepositoryUrl { get; }

    Task InitializeAsync(IPluginContext context);
    Dictionary<string, SettingDef> GetSettingDefDict();
}