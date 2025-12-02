using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        public static string[] SystemFontFamilies => CanvasTextFormat.GetSystemFontFamilies().Order().ToArray();

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
    }
}
