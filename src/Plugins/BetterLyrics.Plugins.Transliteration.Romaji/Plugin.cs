using BetterLyrics.Core.Abstractions.Plugins;
using BetterLyrics.Core.Interfaces.Plugins;
using BetterLyrics.Plugins.Transliteration.Romaji.Helpers;

namespace BetterLyrics.Plugins.Transliteration.Romaji
{
    public class Plugin : PluginBase<Config>, ILyricsTransliterator
    {
        public override string Title { get; set; } = "Romaji";

        protected override async Task OnInitializeAsync()
        {
            RomajiHelper.Init(Context.PluginDirectory);
        }

        public Task<string?> GetTransliterationAsync(string text, string targetLangCode, CancellationToken token)
        {
            string? result = null;
            if (targetLangCode == "ja-latin")
            {
                var lines = text.Split("\n");
                result = string.Join("\n", lines.Select(p => string.Join(" ", RomajiHelper.ToRomaji(p).FirstOrDefault()?.Units.Select(q => q.Romaji) ?? [""])));
            }
            return Task.FromResult(result);
        }
    }
}
