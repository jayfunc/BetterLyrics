using System.Globalization;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NLanguageTag;
using NTextCat;
using Pinyin;
using WanaKanaNet;

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

    private static readonly IAssetReaderProvider _assetReaderProvider =
        Ioc.Default.GetRequiredService<IAssetReaderProvider>();

    private static readonly RankedLanguageIdentifierFactory _factory = new();
    private static readonly RankedLanguageIdentifier _identifier;



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
        new(CultureInfo.CurrentUICulture.Name, _localizationService.GetLocalizedString("SettingsPageSystemLanguage")),
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

    static LanguageHelper()
    {
        _identifier = _factory.Load(_assetReaderProvider.GetAssetStreamAsync("Wiki82.profile.xml").Result);
    }

    /// <summary>
    ///     智能检测语言代码，支持识别拼音、粤拼、罗马音
    /// </summary>
    public static LanguageTag? DetectLanguageTag(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var transliterationCode = TryDetectTransliteration(text);
        if (transliterationCode != null) return transliterationCode;

        var guessList = _identifier.Identify(text);
        var bestMatch = guessList?.FirstOrDefault();

        if (bestMatch == null) return null;

        var code = bestMatch.Item1.Iso639_2T;

        return code switch
        {
            "simple" => EnglishCode,
            "zh" => MandarinChineseCode,
            "zh_classical" => MandarinChineseCode,
            "zh_yue" => YueChineseCode,
            _ => LanguageTag.TryParse(code, out var tag) ? tag : null
        };
    }

    public static LanguageTag? DetectLanguageTag(IEnumerable<string> lines)
    {
        Dictionary<LanguageTag?, int> tagCount = [];
        int cantoneseFeatureCount = 0;

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && CantoneseFeatureRegex().IsMatch(line))
            {
                cantoneseFeatureCount++;
            }

            var tag = DetectLanguageTag(line);
            if (tag != null)
            {
                if (!tagCount.ContainsKey(tag)) tagCount[tag] = 0;
                tagCount[tag]++;
            }
        }

        if (tagCount.Count == 0) return null;

        var bestCode = tagCount.OrderByDescending(kv => kv.Value).First().Key;

        // If the detected language is Mandarin but we found strong Cantonese features in at least a few lines,
        // it's highly likely to be Cantonese because Mandarin rarely uses these specific characters.
        if (bestCode == MandarinChineseCode && cantoneseFeatureCount >= 2)
        {
            return YueChineseCode;
        }

        return bestCode;
    }

    /// <summary>
    ///     尝试识别音译系统 (拼音/粤拼/罗马音)
    /// </summary>
    private static LanguageTag? TryDetectTransliteration(string text)
    {
        if (PinyinToneRegex().IsMatch(text)) return MandarinChineseLatnTag;

        var numberMatches = NumberedToneRegex().Matches(text);
        if (numberMatches.Count > 0)
        {
            foreach (Match match in numberMatches)
                if (match.Value.EndsWith("6"))
                    return YueChineseLatnTag;
            return MandarinChineseLatnTag;
        }

        if (IsLatinOnly(text))
        {
            if (EnglishBlockerRegex().IsMatch(text)) return null;

            var romajiScore = RomajiFeatureRegex().Matches(text).Count;
            var romajaScore = RomajaFeatureRegex().Matches(text).Count;

            if (romajaScore > romajiScore && romajaScore > 0) return KoreanLatnTag;
            if (romajiScore > 0) return JapaneseLatnTag;
        }

        return null;
    }

    private static bool IsLatinOnly(string text)
    {
        return text.All(c =>
            c < 128 && (char.IsLetter(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsDigit(c)));
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

    public static string GetOrderChar(string text)
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
            if (sourceTag.Value.Script == targetTag.Value.Script)
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

    [GeneratedRegex(
        @"\b(the|and|for|that|this|with|you|are|not|what|all|have|one|can|just|but|was)\b|ing\b|tion\b|ment\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EnglishBlockerRegex();

    [GeneratedRegex(
        @"(tsu|shi|chi|kyo|sho|chu|ryu|gyo|byo|myo|nyo|hyo|ja|ju|jo|kya|kyu|sha|shu|cha)\w*|\b(wa|wo|no|ni|ga|de|to|kara|made|yori|kara|he)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RomajiFeatureRegex();

    [GeneratedRegex(@"[a-z]+[1-6]\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NumberedToneRegex();

    [GeneratedRegex(@"\b(sarang|neoun|gaseum|nunmul|joha|neoreul|naega|niga|mian|gomawo|hajiman|geurae|bogo|shipeo)\b|(eo|eu|yae|yeo|kk|tt|pp|jj|ui|wae|weo)\w*|\b[a-z]+(k|m|ng|l|p|t)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RomajaFeatureRegex();

    [GeneratedRegex(@"[āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]", RegexOptions.Compiled)]
    private static partial Regex PinyinToneRegex();

    [GeneratedRegex(@"[嘅喺唔咁哋咗嚟睇嘢佢乜冇畀吖㗎啱啲掟]", RegexOptions.Compiled)]
    private static partial Regex CantoneseFeatureRegex();
}