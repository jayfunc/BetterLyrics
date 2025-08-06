using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Helpers.General;
using NTextCat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TinyPinyin;
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

        public static string? DetectLanguageCode(string? text)
        {
            if (text == null) return null;

            var guessList = _identifier.Identify(text);
            string? code = guessList?.FirstOrDefault()?.Item1.Iso639_2T;
            code = code switch
            {
                "simple" => "en",
                "zh_classical" => "zh-Hant",
                "zh_yue" => "zh-Hant",
                "zh" => "zh-Hans",
                _ => code
            };
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

        public static string GetOrderChar(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "#";
            char c = text.ElementAtOrDefault(0);
            if (char.IsLetter(c) && c < 128)
                return char.ToUpper(c).ToString();

            if (PinyinHelper.IsChinese(c))
            {
                return PinyinHelper.GetPinyinInitials($"{c}");
            }

            return "#";
        }
    }
}
