using System.Globalization;

namespace BetterLyrics.Core.Models;

public class ExtendedLanguage
{
    public ExtendedLanguage(string languageCode, string? name = null)
    {
        LanguageCode = languageCode;
        Name = name ?? new CultureInfo(languageCode).NativeName;
    }

    public string Name { get; private set; }
    public string LanguageCode { get; private set; }
}