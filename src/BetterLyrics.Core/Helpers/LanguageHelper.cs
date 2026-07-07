using System.Globalization;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using NTextCat;
using Pinyin;
using WanaKanaNet;

namespace BetterLyrics.Core.Helpers;

public static partial class LanguageHelper
{
    public const string ChineseCode = "zh";
    public const string JapaneseCode = "ja";
    public const string EnglishCode = "en";

    public const string PinyinCode = "zh-cmn-pinyin";
    public const string JyutpingCode = "zh-yue-jyutping";
    public const string RomanCode = "ja-latin";

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
    public static string? DetectLanguageCode(string? text)
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
                if (!codeCount.ContainsKey(code)) codeCount[code] = 0;
                codeCount[code]++;
            }
        }

        if (codeCount.Count == 0) return null;

        return codeCount.OrderByDescending(kv => kv.Value).First().Key;
    }

    /// <summary>
    ///     尝试识别音译系统 (拼音/粤拼/罗马音)
    /// </summary>
    private static string? TryDetectTransliteration(string text)
    {
        if (PinyinToneRegex().IsMatch(text)) return PinyinCode;

        var numberMatches = NumberedToneRegex().Matches(text);
        if (numberMatches.Count > 0)
        {
            foreach (Match match in numberMatches)
                if (match.Value.EndsWith("6"))
                    return JyutpingCode;
            return PinyinCode;
        }

        if (IsLatinOnly(text))
        {
            if (EnglishBlockerRegex().IsMatch(text)) return null;

            var romajiScore = RomajiFeatureRegex().Matches(text).Count;

            if (romajiScore > 0) return RomanCode;
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

    public static string GetLanguageScriptDisplayName(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return "";
        try
        {
            if (IsPhoneticCode(tag)) return GetDisplayName(tag);

            return new CultureInfo(tag).DisplayName;
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
        @"\b(tsu|shi|chi|ka|ko|sa|su|se|ta|te|na|ni|nu|ne|ha|fu|ho|ma|mi|mu|mo|ya|yu|yo|ra|ri|ru|re|ro|wa|wo|nn|desu|masu)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RomajiFeatureRegex();

    [GeneratedRegex(@"[a-z]+[1-6]\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NumberedToneRegex();

    [GeneratedRegex(@"[āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]", RegexOptions.Compiled)]
    private static partial Regex PinyinToneRegex();
}