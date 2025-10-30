using BetterLyrics.WinUI3.Helper;
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

        public static List<Language> SupportedTranslationTargetLanguages { get; set; } =
        [
            new Language("ar"),
            new Language("az"),

            new Language("bg"),
            new Language("bn"),

            new Language("ca"),
            new Language("cs"),

            new Language("da"),
            new Language("de"),

            new Language("el"),
            new Language("en"),
            new Language("eo"),
            new Language("es"),
            new Language("et"),
            new Language("eu"),

            new Language("fa"),
            new Language("fi"),
            new Language("fr"),

            new Language("ga"),
            new Language("gl"),

            new Language("he"),
            new Language("hi"),
            new Language("hu"),

            new Language("id"),
            new Language("it"),

            new Language("ja"),

            new Language("ko"),
            new Language("ky"),

            new Language("lt"),
            new Language("lv"),

            new Language("ms"),

            new Language("nb"),
            new Language("nl"),

            new Language("pt-BR"),
            new Language("pl"),
            new Language("pt"),

            new Language("ro"),
            new Language("ru"),

            new Language("sk"),
            new Language("sl"),
            new Language("sq"),
            new Language("sr"),
            new Language("sv"),

            new Language("th"),
            new Language("tl"),
            new Language("tr"),

            new Language("uk"),
            new Language("ur"),

            new Language("vi"),

            new Language("zh"),
        ];

        public static List<Language> SupportedDisplayLanguages { get; set; } = ApplicationLanguages.ManifestLanguages.Select(x => new Language(x)).ToList();

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
            var found = SupportedTranslationTargetLanguages.Find(x => ApplicationLanguages.Languages.FirstOrDefault()?.Contains(x.LanguageTag) == true);
            if (found == null)
            {
                return "en";
            }
            else
            {
                return found.LanguageTag;
            }
        }

        public static string GetDefaultDisplayLanguageCode()
        {
            return ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
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
