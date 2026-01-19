using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        private static List<ExtendedFontFamily>? _fontCache;

        public static async Task<List<ExtendedFontFamily>> GetSystemFontFamiliesAsync()
        {
            if (_fontCache != null)
            {
                return _fontCache;
            }

            _fontCache = await Task.Run(() => LoadFontsInternal());

            return _fontCache;
        }

        private static List<ExtendedFontFamily> LoadFontsInternal()
        {
            var fontList = new List<ExtendedFontFamily>();
            var addedFamilyNames = new HashSet<string>();

            var systemFontSet = CanvasFontSet.GetSystemFontSet();

            string userLangPrefix = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

            foreach (var font in systemFontSet.Fonts)
            {
                string? familyNameID = "";

                if (!font.FamilyNames.TryGetValue("en-us", out familyNameID))
                {
                    familyNameID = font.FamilyNames.FirstOrDefault().Value;
                }

                if (string.IsNullOrEmpty(familyNameID) || addedFamilyNames.Contains(familyNameID))
                    continue;

                string displayName = "";

                var localizedStrings = font.GetInformationalStrings(CanvasFontInformation.PreferredFamilyNames);

                if (localizedStrings == null || localizedStrings.Count == 0)
                {
                    localizedStrings = font.FamilyNames;
                }

                displayName = FindBestUpdatedMatch(localizedStrings, userLangPrefix);

                if (!string.IsNullOrEmpty(displayName))
                {
                    fontList.Add(new ExtendedFontFamily
                    {
                        LocalizedFontFamily = displayName,
                        FontFamily = familyNameID
                    });

                    addedFamilyNames.Add(familyNameID);
                }
            }

            return fontList.OrderBy(f => f.LocalizedFontFamily).ToList();
        }

        private static string FindBestUpdatedMatch(IReadOnlyDictionary<string, string> names, string userLangPrefix)
        {
            if (names.Count == 0) return "";

            foreach (var pair in names)
            {
                if (pair.Key.StartsWith(userLangPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            if (names.TryGetValue("en-us", out var enName))
            {
                return enName;
            }

            foreach (var pair in names)
            {
                if (pair.Key.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return names.FirstOrDefault().Value;
        }
    }
}
