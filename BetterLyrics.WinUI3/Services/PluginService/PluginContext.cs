using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Infrastructure;
using BetterLyrics.Core.Interfaces.Services;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginContext : IPluginContext
    {
        private readonly IPluginService _pluginService;

        public string PluginDirectory { get; }
        public IAIService? AIService => _pluginService.GetPlugin<IAIService>();
        public ILocalizer Localizer { get; }
        public Dictionary<string, object> Settings { get; }

        public PluginContext(IPluginService pluginService, string pluginDir, ILocalizer localizer, Dictionary<string, object> settings)
        {
            _pluginService = pluginService;
            Localizer = localizer;
            PluginDirectory = pluginDir;
            Settings = settings;
        }

    }
}
