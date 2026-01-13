using BetterLyrics.Core;
using BetterLyrics.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class PluginInfo : ObservableObject
    {
        [JsonIgnore] public IPlugin? Plugin { get; set; }
        public string Id { get; set; }
        [ObservableProperty] public partial bool IsEnabled { get; set; } = false;

        public PluginInfo() { }

        public PluginInfo(string id)
        {
            Id = id;
        }

        public PluginInfo(IPlugin plugin)
        {
            Plugin = plugin;
            Id = plugin.Id;
        }
    }
}
