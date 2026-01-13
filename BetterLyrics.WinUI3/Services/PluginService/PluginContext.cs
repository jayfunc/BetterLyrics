using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginContext : IPluginContext
    {
        private readonly IPluginService _pluginService;

        public string PluginDirectory { get; }
        public IAIService? AIService => _pluginService.GetPlugin<IAIService>();

        public PluginContext(IPluginService pluginService, string pluginDir)
        {
            _pluginService = pluginService;
            PluginDirectory = pluginDir;
        }

    }
}
