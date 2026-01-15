using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Models.SettingsSchema;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class PluginInfo : ObservableObject
    {
        public string Id { get; set; } = string.Empty;

        public Dictionary<string, object> Settings { get; set; } = new();

        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        [JsonIgnore]
        public IPlugin? Plugin { get; set; }

        [JsonIgnore]
        public bool IsInitialized { get; set; } = false;

        public IEnumerable<SettingDef> SettingsDefinitions =>
            (Plugin as IConfigurable)?.GetSettings() ?? Enumerable.Empty<SettingDef>();

        public PluginInfo() { }

        public PluginInfo(IPlugin plugin)
        {
            Id = plugin.Id;
            Plugin = plugin;

            IsEnabled = true;
        }

        public T? GetSetting<T>(string key, T? defaultValue = default)
        {
            if (Settings.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    try { return element.Deserialize<T>(); } catch { }
                }
                if (value is T typedValue) return typedValue;
            }
            return defaultValue;
        }

    }
}