using System.Text.Json.Serialization;
using BetterLyrics.Sdk.Interfaces.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class PluginInfo : ObservableRecipient
{
    public PluginInfo()
    {
    }

    public PluginInfo(IPlugin plugin)
    {
        Id = plugin.Id;
        Plugin = plugin;

        IsEnabled = true;
    }

    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; }

    [JsonIgnore] public IPlugin? Plugin { get; set; }

    [JsonIgnore] public bool IsInitialized { get; set; } = false;
}