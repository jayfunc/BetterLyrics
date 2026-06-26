using BetterLyrics.Core.Interfaces.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings
{
    public partial class PluginInfo : ObservableRecipient
    {
        public string Id { get; set; } = string.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; }

        [JsonIgnore] public IPlugin? Plugin { get; set; }

        [JsonIgnore] public bool IsInitialized { get; set; } = false;

        public PluginInfo() { }

        public PluginInfo(IPlugin plugin)
        {
            Id = plugin.Id;
            Plugin = plugin;

            IsEnabled = true;
        }

    }
}