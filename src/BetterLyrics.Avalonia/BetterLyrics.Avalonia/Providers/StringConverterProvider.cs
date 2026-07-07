using BetterLyrics.Core.Interfaces.Providers;

namespace BetterLyrics.Avalonia.Providers;

public class StringConverterProvider : IStringConverterProvider
{
    public string RomajiToKanji(string romaji)
    {
        return romaji;
    }
}