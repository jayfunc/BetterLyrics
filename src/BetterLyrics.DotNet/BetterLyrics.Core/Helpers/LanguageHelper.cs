using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lingua;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NLanguageTag;
using Panlingo.LanguageIdentification.CLD3;
using Pinyin;
using System.Globalization;
using System.Text.RegularExpressions;
using WanaKanaNet;
using Language = NLanguageTag.Language;

namespace BetterLyrics.Core.Helpers;

public static partial class LanguageHelper
{
    // https://r12a.github.io/app-subtags/

    public static readonly LanguageTag EnglishCode = new(Language.EN);

    public static readonly LanguageTag MandarinChineseCode = new(Language.CMN);
    public static readonly LanguageTag MandarinChineseLatnTag = new(Language.CMN, Script.Latn);

    public static readonly LanguageTag YueChineseCode = new(Language.YUE);
    public static readonly LanguageTag YueChineseLatnTag = new(Language.YUE, Script.Latn);

    public static readonly LanguageTag JapaneseCode = new(Language.JA);
    public static readonly LanguageTag JapaneseLatnTag = new(Language.JA, Script.Latn);

    public static readonly LanguageTag KoreanCode = new(Language.KO);
    public static readonly LanguageTag KoreanLatnTag = new(Language.KO, Script.Latn);

    private static readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private static readonly IStringConverterProvider _stringConverterProvider =
        Ioc.Default.GetRequiredService<IStringConverterProvider>();

    private static LanguageDetector? _detector;

    public static readonly List<ExtendedLanguage> SupportedTranslationTargetLanguages =
    [
        new("ar"), new("az"),
        new("bg"), new("bn"),
        new("ca"), new("cs"),
        new("da"), new("de"),
        new("el"), new("en"),
        new("eo"), new("es"),
        new("et"), new("eu"),
        new("fa"), new("fi"),
        new("fr"), new("ga"),
        new("gl"), new("he"),
        new("hi"), new("hu"),
        new("id"), new("it"),
        new("ja"), new("ko"),
        new("ky"), new("lt"),
        new("lv"), new("ms"),
        new("nb"), new("nl"),
        new("pt-BR"), new("pl"),
        new("pt"), new("ro"),
        new("ru"), new("sk"),
        new("sl"), new("sq"),
        new("sr"), new("sv"),
        new("th"), new("tl"),
        new("tr"), new("uk"),
        new("ur"), new("vi"),
        new("zh")
    ];

    public static readonly List<ExtendedLanguage> SupportedDisplayLanguages =
    [
        new("", _localizationService.GetLocalizedString("SettingsPageSystemLanguage")),
        new("ar"), new("de"),
        new("en"), new("es"),
        new("fr"), new("hi"),
        new("id"), new("it"),
        new("ja"), new("ko"),
        new("ms"), new("pt"),
        new("ru"), new("th"),
        new("vi"), new("zh-Hans"),
        new("zh-Hant")
    ];

    public static async Task InitIdentifierAsync()
    {
        _detector = LanguageDetectorBuilder.FromAllLanguages().Build();
    }

    /// <summary>
    /// 智能检测语言代码，支持识别拼音、粤拼、罗马音
    /// </summary>
    public static LanguageTag? DetectLanguageTag(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        using var cld3Detector = new CLD3Detector(minNumBytes: 0, maxNumBytes: 10000);
        return DetectLanguageTagCore(text, cld3Detector);
    }

    /// <summary>
    /// 逐行识别后按信息量加权投票得出整体语言
    /// </summary>
    public static LanguageTag? DetectLanguageTag(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var samples = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();

        if (samples.Count == 0) return null;
        if (samples.Count == 1) return DetectLanguageTag(samples[0]);

        var votes = new Dictionary<string, (LanguageTag Tag, int Weight, int Count)>();

        using (var cld3Detector = new CLD3Detector(minNumBytes: 0, maxNumBytes: 10000))
        {
            foreach (var sample in samples)
            {
                var tag = DetectLanguageTagCore(sample, cld3Detector);
                if (tag == null) continue;

                int weight = GetInformationWeight(sample);
                if (weight <= 0) continue;

                string key = GetVoteKey(tag.Value);
                votes[key] = votes.TryGetValue(key, out var vote)
                    ? (vote.Tag, vote.Weight + weight, vote.Count + 1)
                    : (tag.Value, weight, 1);
            }
        }

        if (votes.Count == 0)
        {
            // 逐行均无法识别时，回退到整体识别
            return DetectLanguageTag(string.Join("\n", samples));
        }

        return votes.Values
            .OrderByDescending(vote => vote.Weight)
            .ThenByDescending(vote => vote.Count)
            .First()
            .Tag;
    }

    private static LanguageTag? DetectLanguageTagCore(string text, CLD3Detector cld3Detector)
    {
        // 1. 拦截粤语特有汉字
        if (CantoneseFeatureRegex().IsMatch(text))
        {
            return YueChineseCode;
        }

        // 2. 拦截带数字声调的粤拼/拼音
        var numberMatches = NumberedToneRegex().Matches(text);
        if (numberMatches.Count > 0)
        {
            foreach (Match match in numberMatches)
            {
                if (match.Value.EndsWith("6"))
                    return YueChineseLatnTag;
            }
            return MandarinChineseLatnTag;
        }

        var cld3Result = cld3Detector.PredictLanguage(text);
        if (cld3Result.IsReliable)
        {
            var code = cld3Result.Language;

            if (code.Equals("zh-Latn", StringComparison.OrdinalIgnoreCase)) return MandarinChineseLatnTag;
            if (code.Equals("ja-Latn", StringComparison.OrdinalIgnoreCase)) return JapaneseLatnTag;
            if (code.Equals("ko-Latn", StringComparison.OrdinalIgnoreCase)) return KoreanLatnTag;

            if (code == "zh") return MandarinChineseCode;
            if (LanguageTag.TryParse(code, out var tag))
            {
                return tag;
            }
        }

        var guessLang = _detector?.DetectLanguageOf(text);

        if (guessLang == null) return null;

        var codeLingua = guessLang.Value.IsoCode6391().ToString().ToLower();

        if (codeLingua == "zh") return MandarinChineseCode;

        if (LanguageTag.TryParse(codeLingua, out var tagLingua))
        {
            return tagLingua;
        }

        return null;
    }

    /// <summary>
    /// 投票分组键。汉字的不同书写形式（zh / zh-Hant / cmn）视为同一种语言合并计票，
    /// 而拉丁转写（cmn-Latn、ja-Latn…）保持独立，以便区分原文与音译。
    /// </summary>
    private static string GetVoteKey(LanguageTag tag)
    {
        var code = tag.ToString();

        if (code.Equals("cmn", StringComparison.OrdinalIgnoreCase) ||
            (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase) &&
             !code.StartsWith("zh-Latn", StringComparison.OrdinalIgnoreCase)))
        {
            return MandarinChineseCode.ToString();
        }

        return code;
    }

    /// <summary>
    /// 估算一行文本的信息量权重：以空格分词的文字按词计，CJK 等表意文字按字计。
    /// </summary>
    private static int GetInformationWeight(string text)
    {
        var weight = 0;
        var inWord = false;

        foreach (var ch in text)
        {
            if (!char.IsLetter(ch))
            {
                inWord = false;
                continue;
            }

            // CJK、假名、谚文、假名重复符号等表意/音节文字，一字即一个单位
            if (ch >= FirstCjkRadical)
            {
                weight++;
                inWord = false;
            }
            else if (!inWord)
            {
                // 拉丁、希腊、西里尔等以空格分词的文字，一个词计一个单位
                weight++;
                inWord = true;
            }
        }

        return weight;
    }


    public static bool IsCJK(string text)
    {
        return Lyricify.Lyrics.Helpers.General.StringHelper.IsCJK(text);
    }

    public static bool IsCJK(char ch)
    {
        return IsCJK(ch.ToString());
    }

    public static bool IsRomaji(string text)
    {
        return WanaKana.IsRomaji(text);
    }

    public static bool IsHanzi(char ch)
    {
        return Pinyin.Pinyin.Instance.IsHanzi(ch.ToString());
    }

    public static bool IsHanzi(string text)
    {
        return Pinyin.Pinyin.Instance.IsHanzi(text);
    }

    public static string GetDefaultTargetTranslationLanguageCode()
    {
        var currentLang = CultureInfo.CurrentUICulture.Name;
        var found = SupportedTranslationTargetLanguages.Find(x => currentLang?.Contains(x.LanguageCode) == true);
        return found?.LanguageCode ?? "en";
    }

    public static string GetOrderChar(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "#";

        var c = text[0];

        if (char.IsLetter(c) && c < 128)
            return char.ToUpperInvariant(c).ToString();

        if (IsHanzi(c))
        {
            var pinyin = ConvertHanziToPinyin(c.ToString(), ManTone.Style.NORMAL);
            return pinyin.FirstOrDefault().ToString().ToUpperInvariant();
        }

        return "#";
    }

    public static bool IsPhoneticTag(LanguageTag? tag) => tag?.Script == Script.Latn;

    public static LanguageTag? GetPhoneticTag(LanguageTag? tag)
    {
        if (tag == null) return null;
        if (tag.Value.Language != null)
        {
            return new LanguageTag(tag.Value.Language, Script.Latn);
        }
        return null;
    }

    public static bool IsLanguageMatch(LanguageTag? sourceTag, string? targetCode, bool fuzzy = false)
    {
        if (sourceTag != null && LanguageTag.TryParse(targetCode, out var targetTag))
        {
            return IsLanguageMatch(sourceTag, targetTag, fuzzy);
        }

        return false;
    }

    public static bool IsLanguageMatch(string? sourceCode, LanguageTag? targetTag, bool fuzzy = false)
    {
        if (LanguageTag.TryParse(sourceCode, out var sourceTag) && targetTag != null)
        {
            return IsLanguageMatch(sourceTag, targetTag, fuzzy);
        }

        return false;
    }

    public static bool IsLanguageMatch(LanguageTag? sourceTag, LanguageTag? targetTag, bool fuzzy = false)
    {
        if (sourceTag != null && targetTag != null)
        {
            if (sourceTag.Value.Script == targetTag.Value.Script ||
                (fuzzy && (sourceTag.Value.Script == null || targetTag.Value.Script == null) &&
                 sourceTag.Value.Script != Script.Latn && targetTag.Value.Script != Script.Latn))
            {
                if (fuzzy)
                {
                    return (sourceTag.Value.Language?.Macrolanguage ?? sourceTag.Value.Language) == (targetTag.Value.Language?.Macrolanguage ?? targetTag.Value.Language);
                }
                else
                {
                    return sourceTag.Value.Language == targetTag.Value.Language;
                }
            }
        }

        return false;
    }

    public static string ConvertHanziToPinyin(string text, ManTone.Style style = ManTone.Style.TONE)
    {
        return Pinyin.Pinyin.Instance.HanziToPinyin(text, style).ToStr();
    }

    public static string ConvertHanziToJyutping(string text)
    {
        return Jyutping.Instance.HanziToPinyin(text).ToStr();
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
        return _stringConverterProvider.RomajiToKanji(romaji);
    }

    /// <summary>
    /// CJK 部首区的起始码位（U+2E80），此码位及之后的字符按“一字一单位”计算信息量。
    /// </summary>
    private const char FirstCjkRadical = '\u2E80';

    [GeneratedRegex(@"[a-z]+[1-6]\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NumberedToneRegex();

    [GeneratedRegex(@"[嘅喺唔咁哋咗嚟睇嘢佢乜冇畀吖㗎啱啲掟]", RegexOptions.Compiled)]
    private static partial Regex CantoneseFeatureRegex();
}
