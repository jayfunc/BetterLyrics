using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Models.SettingsSchema;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.Models.Settings
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