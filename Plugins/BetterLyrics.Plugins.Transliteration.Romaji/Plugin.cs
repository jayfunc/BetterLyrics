using BetterLyrics.Core;
using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.Plugins.Romaji.Helpers;

namespace BetterLyrics.Plugins.Transliteration.Romaji
{
    public class Plugin : PluginBase, ILyricsTransliterator
    {
        public override string Name => "Romaji";
        public override string Description => "Convert Japanese lyrics to Romaji transliteration";

        public override void OnLoad(IPluginContext context)
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

        public override void OnUnload()
        {
        }
    }
}
