using LanguageDetection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public class LanguageDetectionHelper
    {
        private static readonly LanguageDetector _detector = new();

        static LanguageDetectionHelper()
        {
            _detector.AddAllLanguages();
        }

        private static string? ThreeLetterToTwoLetter(string threeLetterCode)
        {
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (string.Equals(ci.ThreeLetterISOLanguageName, threeLetterCode, StringComparison.OrdinalIgnoreCase))
                {
                    return ci.TwoLetterISOLanguageName;
                }
            }
            return null;
        }

        public static string? DetectLanguageCode(string? text)
        {
            if (text == null) return null;

            return ThreeLetterToTwoLetter(_detector.Detect(text));
        }

        public static bool IsCJK(string text)
        {
            return DetectLanguageCode(text) switch
            {
                "zh" or "ja" or "ko" => true,
                _ => false
            };
        }
    }
}
