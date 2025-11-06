using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using NTextCat;
using NTextCat.Commons;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Helper
{
    public class LanguageHelper
    {
        private static readonly RankedLanguageIdentifierFactory _factory = new();
        private static readonly RankedLanguageIdentifier _identifier;
        private static readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public static List<ExtendedLanguage> SupportedTranslationTargetLanguages { get; set; } =
        [
            new ExtendedLanguage("ar"),
            new ExtendedLanguage("az"),

            new ExtendedLanguage("bg"),
            new ExtendedLanguage("bn"),

            new ExtendedLanguage("ca"),
            new ExtendedLanguage("cs"),

            new ExtendedLanguage("da"),
            new ExtendedLanguage("de"),

            new ExtendedLanguage("el"),
            new ExtendedLanguage("en"),
            new ExtendedLanguage("eo"),
            new ExtendedLanguage("es"),
            new ExtendedLanguage("et"),
            new ExtendedLanguage("eu"),

            new ExtendedLanguage("fa"),
            new ExtendedLanguage("fi"),
            new ExtendedLanguage("fr"),

            new ExtendedLanguage("ga"),
            new ExtendedLanguage("gl"),

            new ExtendedLanguage("he"),
            new ExtendedLanguage("hi"),
            new ExtendedLanguage("hu"),

            new ExtendedLanguage("id"),
            new ExtendedLanguage("it"),

            new ExtendedLanguage("ja"),

            new ExtendedLanguage("ko"),
            new ExtendedLanguage("ky"),

            new ExtendedLanguage("lt"),
            new ExtendedLanguage("lv"),

            new ExtendedLanguage("ms"),

            new ExtendedLanguage("nb"),
            new ExtendedLanguage("nl"),

            new ExtendedLanguage("pt-BR"),
            new ExtendedLanguage("pl"),
            new ExtendedLanguage("pt"),

            new ExtendedLanguage("ro"),
            new ExtendedLanguage("ru"),

            new ExtendedLanguage("sk"),
            new ExtendedLanguage("sl"),
            new ExtendedLanguage("sq"),
            new ExtendedLanguage("sr"),
            new ExtendedLanguage("sv"),

            new ExtendedLanguage("th"),
            new ExtendedLanguage("tl"),
            new ExtendedLanguage("tr"),

            new ExtendedLanguage("uk"),
            new ExtendedLanguage("ur"),

            new ExtendedLanguage("vi"),

            new ExtendedLanguage("zh"),
        ];

        public static List<ExtendedLanguage> SupportedDisplayLanguages { get; set; } =
        [
            new ExtendedLanguage("", _resourceService.GetLocalizedString("SettingsPageSystemLanguage")),
            new ExtendedLanguage("en-US", "English"),
            new ExtendedLanguage("ja-JP"),
            new ExtendedLanguage("ko-KR"),
            new ExtendedLanguage("zh-CN", "简体中文"),
            new ExtendedLanguage("zh-TW", "繁體中文"),
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
                "zh_classical" => "zh",
                "zh_yue" => "zh",
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

        public static string GetDefaultTargetTranslationLanguageCode()
        {
            var found = SupportedTranslationTargetLanguages.Find(x => ApplicationLanguages.Languages.FirstOrDefault()?.Contains(x.LanguageCode) == true);
            if (found == null)
            {
                return "en";
            }
            else
            {
                return found.LanguageCode;
            }
        }

        public static string GetOrderChar(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "#";
            char c = text.ElementAtOrDefault(0);
            if (char.IsLetter(c) && c < 128)
                return char.ToUpper(c).ToString();

            if (IsHanzi(c.ToString()))
            {
                return PhoneticHelper.ToPinyin(c.ToString(), Pinyin.ManTone.Style.NORMAL).ToUpper().FirstOrDefault().ToString();
            }

            return "#";
        }

        public static bool IsHanzi(char ch)
        {
            return IsHanzi(ch.ToString());
        }

        public static bool IsHanzi(string text)
        {
            return Pinyin.Pinyin.Instance.IsHanzi(text);
        }
    }
}
