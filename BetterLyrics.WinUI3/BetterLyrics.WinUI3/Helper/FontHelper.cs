using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        public static string GetLocalizedFontFamilyName(string sourceName, string langCode)
        {
            if (langCode == "")
            {
                langCode = CultureInfo.CurrentCulture.Name;
            }

            foreach (var font in Fonts.SystemFontFamilies)
            {
                if (font.FamilyNames.TryGetValue(XmlLanguage.GetLanguage("en-us"), out string englishFamilyName) && englishFamilyName == sourceName)
                {
                    if (font.FamilyNames.ContainsKey(XmlLanguage.GetLanguage(langCode)))
                    {
                        if (font.FamilyNames.TryGetValue(XmlLanguage.GetLanguage(langCode), out string localizedFamilyName))
                        {
                            return localizedFamilyName;
                        }
                    }
                }
            }

            return sourceName;
        }

        public static List<string> GetSystemFontFamilies()
        {
            List<string> fontFamilies = new();

            foreach (var font in Fonts.SystemFontFamilies)
            {
                if (font.FamilyNames.TryGetValue(XmlLanguage.GetLanguage("en-us"), out string englishFamilyName))
                {
                    fontFamilies.Add(englishFamilyName);
                }
            }

            return fontFamilies.Order().ToList();
        }
    }
}
