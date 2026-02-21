using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NTextCat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WanaKanaNet;
using Windows.Globalization;
using static BetterLyrics.WinUI3.Hooks.ImeHook;

namespace BetterLyrics.WinUI3.Helper
{
    public static partial class LanguageHelper
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
        private static readonly RankedLanguageIdentifierFactory _factory = new();
        private static readonly RankedLanguageIdentifier _identifier;

        public const string ChineseCode = "zh";
        public const string JapaneseCode = "ja";
        public const string EnglishCode = "en";

        public const string PinyinCode = "zh-cmn-pinyin";
        public const string JyutpingCode = "zh-yue-jyutping";
        public const string RomanCode = "ja-latin";

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
            _identifier = _factory.Load(PathHelper.LanguageProfilePath);
        }

        /// <summary>
        /// 智能检测语言代码，支持识别拼音、粤拼、罗马音
        /// </summary>
        public static string? DetectLanguageCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var transliterationCode = TryDetectTransliteration(text);
            if (transliterationCode != null)
            {
                return transliterationCode;
            }

            var guessList = _identifier.Identify(text);
            var bestMatch = guessList?.FirstOrDefault();

            if (bestMatch == null) return null;

            string code = bestMatch.Item1.Iso639_2T;

            return code switch
            {
                "simple" => EnglishCode,
                "zh_classical" => ChineseCode,
                "zh_yue" => ChineseCode,
                _ => code
            };
        }

        public static string? DetectLanguageCode(IEnumerable<string> lines)
        {
            Dictionary<string, int> codeCount = new();
            foreach (var line in lines)
            {
                var code = DetectLanguageCode(line);
                if (code != null)
                {
                    if (!codeCount.ContainsKey(code))
                    {
                        codeCount[code] = 0;
                    }
                    codeCount[code]++;
                }
            }

            if (codeCount.Count == 0) return null;

            return codeCount.OrderByDescending(kv => kv.Value).First().Key;
        }

        /// <summary>
        /// 尝试识别音译系统 (拼音/粤拼/罗马音)
        /// </summary>
        private static string? TryDetectTransliteration(string text)
        {
            if (PinyinToneRegex().IsMatch(text))
            {
                return PinyinCode;
            }

            var numberMatches = NumberedToneRegex().Matches(text);
            if (numberMatches.Count > 0)
            {
                foreach (Match match in numberMatches)
                {
                    if (match.Value.EndsWith("6")) return JyutpingCode;
                }
                return PinyinCode;
            }

            if (IsLatinOnly(text))
            {
                if (EnglishBlockerRegex().IsMatch(text))
                {
                    return null;
                }

                int romajiScore = RomajiFeatureRegex().Matches(text).Count;

                if (romajiScore > 0)
                {
                    return RomanCode;
                }
            }

            return null;
        }

        private static bool IsLatinOnly(string text)
        {
            return text.All(c => c < 128 && (char.IsLetter(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsDigit(c)));
        }

        public static bool IsCJK(string text) => Lyricify.Lyrics.Helpers.General.StringHelper.IsCJK(text);

        public static bool IsCJK(char ch) => IsCJK(ch.ToString());

        public static bool IsRomaji(string text)
        {
            return WanaKana.IsRomaji(text);
        }

        public static bool IsHanzi(char ch) => Pinyin.Pinyin.Instance.IsHanzi(ch.ToString());
        public static bool IsHanzi(string text) => Pinyin.Pinyin.Instance.IsHanzi(text);

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

            if (char.IsLetter(c) && c < 128)
                return char.ToUpperInvariant(c).ToString();

            if (IsHanzi(c))
            {
                var pinyin = ConvertHanziToPinyin(c.ToString(), Pinyin.ManTone.Style.NORMAL);
                return pinyin.FirstOrDefault().ToString().ToUpperInvariant();
            }

            return "#";
        }

        public static string GetLanguageScriptDisplayName(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return "";
            try
            {
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
                _ => code
            };
        }

        public static string ConvertHanziToPinyin(string text, Pinyin.ManTone.Style style = Pinyin.ManTone.Style.TONE)
        {
            return Pinyin.Pinyin.Instance.HanziToPinyin(text, style).ToStr();
        }

        public static string ConvertHanziToJyutping(string text)
        {
            return Pinyin.Jyutping.Instance.HanziToPinyin(text).ToStr();
        }

        public static string ConvertTCToSC(string text)
        {
            return ChineseConverter.Convert(text, ChineseConversionDirection.TraditionalToSimplified);
        }

        public static string ConvertSCToTC(string text)
        {
            return ChineseConverter.Convert(text, ChineseConversionDirection.SimplifiedToTraditional);
        }

        public static string ConvertRomajiToKanji(string romaji)
        {
            string hiragana = WanaKana.ToKana(romaji, new WanaKanaOptions() { ImeMode = ImeMode.ToHiragana }).Replace(" ", "");

            IFELanguage? ife = null;
            IntPtr resultPtr = IntPtr.Zero;

            try
            {
                Type? imeType = Type.GetTypeFromProgID("MSIME.Japan");
                if (imeType == null) return romaji; // 没装日文输入法，原样返回

                ife = (IFELanguage?)Activator.CreateInstance(imeType);
                if (ife?.Open() != 0) return romaji;

                int hr = ife.GetJMorphResult(
                    (uint)ConversionRequest.Conversion,
                    (uint)ConversionMode.HiraganaOut,
                    hiragana.Length,
                    hiragana,
                    IntPtr.Zero,
                    out resultPtr
                );

                if (hr == 0 && resultPtr != IntPtr.Zero)
                {
                    MorphResult result = Marshal.PtrToStructure<MorphResult>(resultPtr);

                    string? bestString = null;

                    if (result.PtrToOutputString != IntPtr.Zero)
                    {
                        bestString = Marshal.PtrToStringUni(result.PtrToOutputString, result.OutputLength);
                    }

                    Marshal.FreeCoTaskMem(resultPtr);

                    return string.IsNullOrEmpty(bestString) ? romaji : bestString;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IME Error: " + ex.Message);
            }
            finally
            {
                ife?.Close();
            }

            return romaji;
        }

        [GeneratedRegex(@"\b(the|and|for|that|this|with|you|are|not|what|all|have|one|can|just|but|was)\b|ing\b|tion\b|ment\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex EnglishBlockerRegex();

        [GeneratedRegex(@"\b(tsu|shi|chi|ka|ko|sa|su|se|ta|te|na|ni|nu|ne|ha|fu|ho|ma|mi|mu|mo|ya|yu|yo|ra|ri|ru|re|ro|wa|wo|nn|desu|masu)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex RomajiFeatureRegex();

        [GeneratedRegex(@"[a-z]+[1-6]\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex NumberedToneRegex();

        [GeneratedRegex(@"[āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]", RegexOptions.Compiled)]
        private static partial Regex PinyinToneRegex();
    }
}