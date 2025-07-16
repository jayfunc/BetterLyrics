using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Helpers.General;
using NTextCat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Services
{
    public class LanguageHelper
    {
        private static readonly RankedLanguageIdentifierFactory _factory = new();
        private static readonly RankedLanguageIdentifier _identifier;
        private static readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public static List<Models.LanguageInfo> SupportedTargetLanguages =>
        [
            new Models.LanguageInfo("ar", "العربية"),
            new Models.LanguageInfo("az", "Azərbaycan dili"),
            new Models.LanguageInfo("zh-Hans", "简体中文"),
            new Models.LanguageInfo("zh-Hant", "繁體中文"),
            new Models.LanguageInfo("cs", "Čeština"),
            new Models.LanguageInfo("da", "Dansk"),
            new Models.LanguageInfo("nl", "Nederlands"),
            new Models.LanguageInfo("en", "English"),
            new Models.LanguageInfo("eo", "Esperanto"),
            new Models.LanguageInfo("fi", "Suomi"),
            new Models.LanguageInfo("fr", "Français"),
            new Models.LanguageInfo("de", "Deutsch"),
            new Models.LanguageInfo("el", "Ελληνικά"),
            new Models.LanguageInfo("he", "עברית"),
            new Models.LanguageInfo("hi", "हिन्दी"),
            new Models.LanguageInfo("hu", "Magyar"),
            new Models.LanguageInfo("id", "Bahasa Indonesia"),
            new Models.LanguageInfo("ga", "Gaeilge"),
            new Models.LanguageInfo("it", "Italiano"),
            new Models.LanguageInfo("ja", "日本語"),
            new Models.LanguageInfo("ko", "한국어"),
            new Models.LanguageInfo("fa", "فارسی"),
            new Models.LanguageInfo("pl", "Polski"),
            new Models.LanguageInfo("pt", "Português"),
            new Models.LanguageInfo("ru", "Русский"),
            new Models.LanguageInfo("sk", "Slovenčina"),
            new Models.LanguageInfo("es", "Español"),
            new Models.LanguageInfo("sv", "Svenska"),
            new Models.LanguageInfo("tr", "Türkçe"),
            new Models.LanguageInfo("uk", "Українська"),
            new Models.LanguageInfo("vi", "Tiếng Việt"),
        ];

        static LanguageHelper()
        {
            _identifier = _factory.Load(PathHelper.LanguageProfilePath);
        }

        private static string? ThreeLetterToTwoLetter(string? threeLetterCode)
        {
            if (threeLetterCode == null) return null;

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

            string? code = ThreeLetterToTwoLetter(_identifier.Identify(text).FirstOrDefault()?.Item1.Iso639_2T);
            if (code != null && code == "zh")
            {
                if (ChineseConverter.ConvertToTraditionalChinese(text) == text)
                {
                    return "zh-Hant";
                }
                else
                {
                    return "zh-Hans";
                }
            }
            return code;
        }

        public static bool IsCJK(string text)
        {
            return DetectLanguageCode(text)?.Substring(0, 2) switch
            {
                "zh" or "ja" or "ko" => true,
                _ => false
            };
        }

        public static string ConvertToCountryCode(string? languageCode)
        {
            if (languageCode == null) return "us";

            return languageCode switch
            {
                "zh" => "cn",
                "zh-Hans" => "cn",
                "zh-Hant" => "tw",
                "ja" => "jp",
                "ko" => "kr",
                _ => "us"
            };
        }

        public static string GetUserTargetLanguageCode()
        {
            return SupportedTargetLanguages[_settingsService.SelectedTargetLanguageIndex].Code;
        }

        public static int GetDefaultTargetLanguageIndex()
        {
            int found = SupportedTargetLanguages.FindIndex(x => ApplicationLanguages.Languages.FirstOrDefault()?.Contains(x.Code) == true);
            if (found == -1) found = 7; // 默认使用英语
            return found;
        }
    }
}
