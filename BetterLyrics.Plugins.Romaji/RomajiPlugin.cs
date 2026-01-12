using BetterLyrics.Core.Interfaces;
using RomajiConverter.Core.Helpers;
using System.Reflection;

namespace BetterLyrics.Plugins.Romaji
{
    public class RomajiPlugin : ILyricsTransliterationPlugin
    {
        public string Id => "jayfunc.romaji";
        public string Name => "Romaji";
        public string Description => "Convert Japanese lyrics to Romaji transliteration.";
        public string Author => "jayfunc";
        public string Version => "1.0.0";
        public DateTime LastUpdated => new DateTime(2026, 1, 12);

        public void OnLoad(IPluginContext context)
        {
            RomajiHelper.Init(context.PluginDirectory);
        }

        public Task<string?> GetTransliterationAsync(string text, string targetLangCode)
        {
            string? result = null;
            if (targetLangCode == "ja-latin")
            {
                var lines = text.Split("\n");
                result = string.Join("\n", lines.Select(p => string.Join(" ", RomajiHelper.ToRomaji(p).FirstOrDefault()?.Units.Select(q => q.Romaji) ?? [""])));
            }
            return Task.FromResult(result);
        }

        public void OnUnload()
        {
        }
    }
}
