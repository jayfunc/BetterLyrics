using BetterLyrics.Core.Abstractions;
using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Core.Interfaces.Infrastructure
{
    public interface IPluginContext
    {
        string PluginDirectory { get; }

        IAIService? AIService { get; }
        ILocalizer Localizer { get; }
        /// <summary>
        /// If you are modifying config in plugin side, please use
        /// <see cref="PluginBase{TConfig}.Config"/>
        /// directly.
        /// </summary>
        IConfigurator Configurator { get; }
    }
}
