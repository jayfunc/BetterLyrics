using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using NTextCat;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Helper
{
    public static class LanguageHelper
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
        private static readonly RankedLanguageIdentifierFactory _factory = new();
        private static readonly RankedLanguageIdentifier _identifier;

        // 常量定义
        public const string ChineseCode = "zh";
        public const string JapaneseCode = "ja";
        public const string EnglishCode = "en";

        public const string PinyinCode = "zh-cmn-pinyin";
        public const string JyutpingCode = "zh-yue-jyutping";
        public const string RomanCode = "ja-latin"; // Romaji

        // 正则表达式预编译 (优化性能)
        // 检测拼音带声调字符 (ā, á, ǎ, à, etc.)
        private static readonly Regex PinyinToneRegex = new(@"[āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]", RegexOptions.Compiled);
        // 检测词尾带数字的情况 (ni3, gwong2)
        private static readonly Regex NumberedToneRegex = new(@"[a-z]+[1-6]\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // 检测日文罗马音特征 (tsu, shi, chi, wo, wa, no, desu...) - 简单的启发式
        private static readonly Regex RomajiFeatureRegex = new(@"\b(tsu|shi|chi|ka|ko|sa|su|se|so|ta|te|to|na|ni|nu|ne|no|ha|hi|fu|he|ho|ma|mi|mu|me|mo|ya|yu|yo|ra|ri|ru|re|ro|wa|wo|nn|desu|masu)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 使用 static readonly 防止被意外修改
        public static readonly List<ExtendedLanguage> SupportedTranslationTargetLanguages =
        [
            new ExtendedLanguage("ar"), new ExtendedLanguage("az"),
            new ExtendedLanguage("bg"), new ExtendedLanguage("bn"),
            new ExtendedLanguage("ca"), new ExtendedLanguage("cs"),
            new ExtendedLanguage("da"), new ExtendedLanguage("de"),
            new ExtendedLanguage("el"), new ExtendedLanguage("en"),
            new ExtendedLanguage("eo"), new ExtendedLanguage("es"),
            new ExtendedLanguage("et"), new ExtendedLanguage("eu"),
            new ExtendedLanguage("fa"), new ExtendedLanguage("fi"),
            new ExtendedLanguage("fr"), new ExtendedLanguage("ga"),
            new ExtendedLanguage("gl"), new ExtendedLanguage("he"),
            new ExtendedLanguage("hi"), new ExtendedLanguage("hu"),
            new ExtendedLanguage("id"), new ExtendedLanguage("it"),
            new ExtendedLanguage("ja"), new ExtendedLanguage("ko"),
            new ExtendedLanguage("ky"), new ExtendedLanguage("lt"),
            new ExtendedLanguage("lv"), new ExtendedLanguage("ms"),
            new ExtendedLanguage("nb"), new ExtendedLanguage("nl"),
            new ExtendedLanguage("pt-BR"), new ExtendedLanguage("pl"),
            new ExtendedLanguage("pt"), new ExtendedLanguage("ro"),
            new ExtendedLanguage("ru"), new ExtendedLanguage("sk"),
            new ExtendedLanguage("sl"), new ExtendedLanguage("sq"),
            new ExtendedLanguage("sr"), new ExtendedLanguage("sv"),
            new ExtendedLanguage("th"), new ExtendedLanguage("tl"),
            new ExtendedLanguage("tr"), new ExtendedLanguage("uk"),
            new ExtendedLanguage("ur"), new ExtendedLanguage("vi"),
            new ExtendedLanguage("zh"),
        ];

        // 这里的初始化依赖 CultureInfo，通常只在启动时运行一次
        public static readonly List<ExtendedLanguage> SupportedDisplayLanguages =
        [
            new ExtendedLanguage(CultureInfo.CurrentUICulture.Name, _localizationService.GetLocalizedString("SettingsPageSystemLanguage")),
            new ExtendedLanguage("ar"), new ExtendedLanguage("de"),
            new ExtendedLanguage("en"), new ExtendedLanguage("es"),
            new ExtendedLanguage("fr"), new ExtendedLanguage("hi"),
            new ExtendedLanguage("id"), new ExtendedLanguage("ja"),
            new ExtendedLanguage("ko"), new ExtendedLanguage("ms"),
            new ExtendedLanguage("pt"), new ExtendedLanguage("ru"),
            new ExtendedLanguage("th"), new ExtendedLanguage("vi"),
            new ExtendedLanguage("zh-Hans"), new ExtendedLanguage("zh-Hant"),
        ];

        static LanguageHelper()
        {
            // 确保 NTextCat Profile 路径正确
            _identifier = _factory.Load(PathHelper.LanguageProfilePath);
        }

        /// <summary>
        /// 智能检测语言代码，支持识别拼音、粤拼、罗马音
        /// </summary>
        public static string? DetectLanguageCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // 1. 优先尝试识别特殊的注音/音译系统 (Pinyin, Jyutping, Romaji)
            // 因为 NTextCat 会把这些都识别成 English 或 Latin，所以要先拦截
            var transliterationCode = TryDetectTransliteration(text);
            if (transliterationCode != null)
            {
                return transliterationCode;
            }

            // 2. 识别不到特殊特征，使用 NTextCat 进行常规自然语言识别
            var guessList = _identifier.Identify(text);
            var bestMatch = guessList?.FirstOrDefault();

            if (bestMatch == null) return null;

            string code = bestMatch.Item1.Iso639_2T;

            // 3. 结果修正 (NTextCat 的一些旧代码映射到标准 ISO 代码)
            return code switch
            {
                "simple" => EnglishCode,
                "zh_classical" => ChineseCode,
                "zh_yue" => ChineseCode, // 如果需要严格区分粤语文本和普通话，这里可以改
                _ => code
            };
        }

        /// <summary>
        /// 尝试识别音译系统 (拼音/粤拼/罗马音)
        /// </summary>
        private static string? TryDetectTransliteration(string text)
        {
            // A. 检测带声调的拼音 (ā, á...) - 这是最强的特征
            if (PinyinToneRegex.IsMatch(text))
            {
                return PinyinCode;
            }

            // B. 检测带数字的拼音/粤拼 (ni3, gwong2)
            // 简单的启发式：通常一行里有多个单词结尾带数字
            var numberMatches = NumberedToneRegex.Matches(text);
            if (numberMatches.Count > 0)
            {
                // 简单的区分逻辑：
                // 粤拼有 6 个声调 (1-6)，普通话拼音通常只有 1-4 (5为轻声)
                // 如果发现了 '6'，极大概率是粤拼
                foreach (Match match in numberMatches)
                {
                    if (match.Value.EndsWith("6")) return JyutpingCode;
                }
                // 否则默认为拼音 (也可以根据需求改为返回 "Unknown Phonetic")
                return PinyinCode;
            }

            // C. 检测日文罗马音 (Romaji)
            // 这是一个难点，因为看起来像英文。
            // 策略：如果是纯拉丁字母，且符合日文发音规则 (CV结构)，且包含特征词
            if (IsLatinOnly(text))
            {
                // 统计罗马音特征词出现的次数
                int romajiScore = RomajiFeatureRegex.Matches(text).Count;

                // 如果句子很短，命中一个特征词就算；如果句子长，需要一定比例
                if (romajiScore > 0)
                {
                    // 这里可以加更复杂的权重判断，简单起见：只要有明显的罗马音特征词，就倾向于罗马音
                    // 尤其是当 NTextCat 可能会误判为其他小语种时
                    return RomanCode;
                }
            }

            return null;
        }

        private static bool IsLatinOnly(string text)
        {
            // 简单检查是否只包含 ASCII 字母和标点
            return text.All(c => c < 128 && (char.IsLetter(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsDigit(c)));
        }

        // 下面保持原有逻辑不变，只做了简单的格式清理

        public static bool IsCJK(string text) => Lyricify.Lyrics.Helpers.General.StringHelper.IsCJK(text);

        public static bool IsCJK(char ch) => IsCJK(ch.ToString());

        public static string GetDefaultTargetTranslationLanguageCode()
        {
            var currentLang = ApplicationLanguages.Languages.FirstOrDefault();
            var found = SupportedTranslationTargetLanguages.Find(x => currentLang?.Contains(x.LanguageCode) == true);
            return found?.LanguageCode ?? "en";
        }

        public static string GetOrderChar(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "#";

            char c = text[0];

            // 英文直接返回首字母
            if (char.IsLetter(c) && c < 128)
                return char.ToUpperInvariant(c).ToString();

            // 汉字转拼音首字母
            if (IsHanzi(c))
            {
                var pinyin = ToPinyin(c.ToString(), Pinyin.ManTone.Style.NORMAL);
                return pinyin.FirstOrDefault().ToString().ToUpperInvariant();
            }

            return "#";
        }

        public static bool IsHanzi(char ch) => Pinyin.Pinyin.Instance.IsHanzi(ch.ToString());
        public static bool IsHanzi(string text) => Pinyin.Pinyin.Instance.IsHanzi(text);

        public static string GetLanguageScriptDisplayName(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "";
            try
            {
                // 处理自定义代码的显示名称
                if (IsPhoneticCode(tag)) return GetDisplayName(tag);

                return new Language(tag).DisplayName;
            }
            catch
            {
                return "";
            }
        }

        public static bool IsPhoneticCode(string code)
        {
            return code is PinyinCode or JyutpingCode or RomanCode;
        }

        public static string GetDisplayName(string code)
        {
            return code switch
            {
                PinyinCode => _localizationService.GetLocalizedString("Pinyin"),
                JyutpingCode => _localizationService.GetLocalizedString("Jyutping"),
                RomanCode => _localizationService.GetLocalizedString("Romaji"),
                _ => code // Fallback
            };
        }

        public static string ToPinyin(string text, Pinyin.ManTone.Style style = Pinyin.ManTone.Style.TONE)
        {
            return Pinyin.Pinyin.Instance.HanziToPinyin(text, style).ToStr();
        }

        public static string ToJyutping(string text)
        {
            return Pinyin.Jyutping.Instance.HanziToPinyin(text).ToStr();
        }
    }
}