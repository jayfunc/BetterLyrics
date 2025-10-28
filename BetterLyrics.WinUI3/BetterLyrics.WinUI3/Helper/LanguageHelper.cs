using BetterLyrics.WinUI3.Helper;
using NTextCat;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Services
{
    public class LanguageHelper
    {
        private static readonly RankedLanguageIdentifierFactory _factory = new();
        private static readonly RankedLanguageIdentifier _identifier;

        public static List<Models.LanguageInfo> SupportedTargetLanguages { get; set; } =
        [
            new Models.LanguageInfo("ar", "العربية"),
            new Models.LanguageInfo("az", "Azərbaycan dili"),

            new Models.LanguageInfo("bg", "Български"),
            new Models.LanguageInfo("bn", "বাংলা"),

            new Models.LanguageInfo("ca", "Català"),
            new Models.LanguageInfo("cs", "Čeština"),

            new Models.LanguageInfo("da", "Dansk"),
            new Models.LanguageInfo("de", "Deutsch"),

            new Models.LanguageInfo("el", "Ελληνικά"),
            new Models.LanguageInfo("en", "English"),
            new Models.LanguageInfo("eo", "Esperanto"),
            new Models.LanguageInfo("es", "Español"),
            new Models.LanguageInfo("et", "Eesti"),
            new Models.LanguageInfo("eu", "Euskara"),

            new Models.LanguageInfo("fa", "فارسی"),
            new Models.LanguageInfo("fi", "Suomi"),
            new Models.LanguageInfo("fr", "Français"),

            new Models.LanguageInfo("ga", "Gaeilge"),
            new Models.LanguageInfo("gl", "Galego"),

            new Models.LanguageInfo("he", "עברית"),
            new Models.LanguageInfo("hi", "हिन्दी"),
            new Models.LanguageInfo("hu", "Magyar"),

            new Models.LanguageInfo("id", "Bahasa Indonesia"),
            new Models.LanguageInfo("it", "Italiano"),

            new Models.LanguageInfo("ja", "日本語"),

            new Models.LanguageInfo("ko", "한국어"),
            new Models.LanguageInfo("ky", "Кыргызча"),

            new Models.LanguageInfo("lt", "Lietuvių"),
            new Models.LanguageInfo("lv", "Latviešu"),

            new Models.LanguageInfo("ms", "Bahasa Melayu"),

            new Models.LanguageInfo("nb", "Norsk bokmål"),
            new Models.LanguageInfo("nl", "Nederlands"),

            new Models.LanguageInfo("pt-BR", "Português (Brasil)"),
            new Models.LanguageInfo("pl", "Polski"),
            new Models.LanguageInfo("pt", "Português"),

            new Models.LanguageInfo("ro", "Română"),
            new Models.LanguageInfo("ru", "Русский"),

            new Models.LanguageInfo("sk", "Slovenčina"),
            new Models.LanguageInfo("sl", "Slovenščina"),
            new Models.LanguageInfo("sq", "Shqip"),
            new Models.LanguageInfo("sr", "Српски"),
            new Models.LanguageInfo("sv", "Svenska"),

            new Models.LanguageInfo("th", "ไทย"),
            new Models.LanguageInfo("tl", "Filipino"),
            new Models.LanguageInfo("tr", "Türkçe"),

            new Models.LanguageInfo("uk", "Українська"),
            new Models.LanguageInfo("ur", "اردو"),

            new Models.LanguageInfo("vi", "Tiếng Việt"),

            new Models.LanguageInfo("zh", "中文"),
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

        public static string GetDefaultTargetLanguageCode()
        {
            var found = SupportedTargetLanguages.Find(x => ApplicationLanguages.Languages.FirstOrDefault()?.Contains(x.Code) == true);
            if (found == null)
            {
                return "en";
            }
            else
            {
                return found.Code;
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
                return ToPinyin(c.ToString(), Pinyin.ManTone.Style.NORMAL).ToUpper().FirstOrDefault().ToString();
            }

            return "#";
        }

        public static string ToRomaji(string text)
        {
            return Kana.Kana.KanaToRomaji(text).ToStr();
        }

        public static string ToPinyin(string text, Pinyin.ManTone.Style style = Pinyin.ManTone.Style.TONE)
        {
            return Pinyin.Pinyin.Instance.HanziToPinyin(text, style).ToStr();
        }

        public static string ToJyutping(string text)
        {
            return Pinyin.Jyutping.Instance.HanziToPinyin(text).ToStr();
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
