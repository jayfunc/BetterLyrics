using System.Globalization;

namespace BetterLyrics.Core.Models;

public class ExtendedLanguage
{
    public ExtendedLanguage(string languageCode, string? nativeName = null)
    {
        LanguageCode = languageCode;
        
        if (nativeName != null)
        {
            NativeName = nativeName;
        }

        try
        {
            var cultureInfo = new CultureInfo(languageCode);
            if (cultureInfo != null)
            {
                NativeName ??= cultureInfo.NativeName;
                DisplayName = cultureInfo.DisplayName;
            }
            else
            {
                NativeName ??= languageCode;
                DisplayName = languageCode;
            }
        }
        catch (CultureNotFoundException)
        {
            NativeName ??= languageCode;
            DisplayName = languageCode;
        }
    }

    public string DisplayName { get; private set; }
    public string NativeName { get; private set; }
    public string LanguageCode { get; private set; }
}