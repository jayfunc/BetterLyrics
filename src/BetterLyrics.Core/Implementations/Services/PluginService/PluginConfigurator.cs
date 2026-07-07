using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Sdk.Enums;
using BetterLyrics.Sdk.Interfaces.Plugins;

namespace BetterLyrics.Core.Implementations.Services.PluginService;

public class PluginConfigurator : IConfigurator
{
    private readonly string _pluginCfgDir;
    private readonly string _pluginCfgPath;
    private Dictionary<string, object> _config = new();

    public PluginConfigurator(string pluginDir)
    {
        _pluginCfgDir = $"{pluginDir}/config";
        _pluginCfgPath = $"{_pluginCfgDir}/config.json";
        EnsureConfigDir();
        ReadSettings();
    }

    public event EventHandler<string, ConfigChangedBy>? OnConfigChanged;

    public object Get(string key, object defaultValue)
    {
        if (_config.TryGetValue(key, out var value)) return value;
        return defaultValue;
    }

    public void Set(string key, object value, ConfigChangedBy configChangedBy)
    {
        _config[key] = value;
        SaveSettings();
        OnConfigChanged?.Invoke(key, configChangedBy);
    }

    private void EnsureConfigDir()
    {
        if (!Directory.Exists(_pluginCfgDir)) Directory.CreateDirectory(_pluginCfgDir);
    }

    private void SaveSettings()
    {
        SettingsIO.SaveSettings(_pluginCfgPath, _config, SourceGenerationContext.Default.DictionaryStringObject);
    }

    private void ReadSettings()
    {
        _config = SettingsIO.ReadSettings(_pluginCfgPath, SourceGenerationContext.Default.DictionaryStringObject);
    }
}