using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Abstractions;

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
