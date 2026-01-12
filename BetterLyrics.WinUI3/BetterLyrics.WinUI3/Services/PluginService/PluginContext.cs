using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginContext : IPluginContext
    {
        private readonly IPluginService _pluginService;

        public string PluginDirectory { get; }
        public IAIServicePlugin? AIService => _pluginService.GetPlugin<IAIServicePlugin>();

        public PluginContext(IPluginService pluginService, string pluginDir)
        {
            _pluginService = pluginService;
            PluginDirectory = pluginDir;
        }

    }
}
