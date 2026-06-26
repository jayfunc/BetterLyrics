using BetterLyrics.Core.Interfaces.Plugins;
using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Core.Implementations.Services.PluginService
{
    public class PluginContext : IPluginContext
    {
        private readonly IPluginService _pluginService;

        public string PluginDirectory { get; }
        public IAIService? AIService => _pluginService.GetPlugin<IAIService>();
        public ILocalizer Localizer { get; }
        public IConfigurator Configurator { get; }
        public Dictionary<string, object> Settings { get; }

        public PluginContext(IPluginService pluginService, string pluginDir, ILocalizer localizer, IConfigurator configurator)
        {
            _pluginService = pluginService;
            Localizer = localizer;
            Configurator = configurator;
            PluginDirectory = pluginDir;
        }

    }
}
