using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Features;

namespace BetterLyrics.Plugins.Translation.LocalAI
{
    public class Plugin : IPlugin, ILyricsTranslator
    {
        public string Id => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public string Author => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public DateTime LastUpdated => throw new NotImplementedException();

        public Task<string?> GetTranslationAsync(string text, string targetLangCode)
        {
            throw new NotImplementedException();
        }

        public void OnLoad(IPluginContext context)
        {
            throw new NotImplementedException();
        }

        public void OnUnload()
        {
            throw new NotImplementedException();
        }
    }
}
