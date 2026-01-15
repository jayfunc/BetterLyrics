using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Core.Interfaces.Infrastructure
{
    public interface IPluginContext
    {
        string PluginDirectory { get; }

        Dictionary<string, object> Settings { get; }

        IAIService? AIService { get; }
        ILocalizer Localizer { get; }
    }
}
