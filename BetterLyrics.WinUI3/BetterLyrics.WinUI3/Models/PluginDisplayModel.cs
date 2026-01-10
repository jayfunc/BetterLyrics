using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class PluginDisplayModel
    {
        public IPlugin Plugin { get; }

        public string Name => Plugin.Name;
        public string Description => Plugin.Description;
        public string Id => Plugin.Id;
        public string Author => Plugin.Author;
        public string Version => "v1.0.0"; // 如果您的接口有 Version 字段就读接口的

        public string Glyph => !string.IsNullOrEmpty(Name) ? Name.Substring(0, 1) : "?";

        public PluginDisplayModel(IPlugin plugin)
        {
            Plugin = plugin;
        }
    }
}
