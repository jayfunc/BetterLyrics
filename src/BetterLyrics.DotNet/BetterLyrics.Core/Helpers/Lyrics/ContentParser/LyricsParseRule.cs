using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using NLanguageTag;

namespace BetterLyrics.Core.Helpers.Lyrics.ContentParser;

public class LyricsParseRule
{
    public bool IsTranslationEnabled { get; }
    public LanguageTag? TargetTranslationTag { get; }
    public HashSet<LanguageTag> AllowedRomanizationTags { get; } = new();

    public ChineseConversion ChineseConversion { get; }
    public bool IsFilterEnabled { get; }

    public LyricsParseRule(TranslationSettings settings)
    {
        IsTranslationEnabled = settings.IsTranslationEnabled;
        if (IsTranslationEnabled && !string.IsNullOrEmpty(settings.SelectedTargetLanguageCode))
        {
            LanguageTag.TryParse(settings.SelectedTargetLanguageCode, out var tag);
            TargetTranslationTag = tag;
        }

        ChineseConversion = settings.ChineseConversion;
        IsFilterEnabled = settings.IsFilterEnabled;

        if (settings.IsMandarinRomanizationEnabled)
            AllowedRomanizationTags.Add(LanguageHelper.MandarinChineseLatnTag);
        if (settings.IsCantoneseRomanizationEnabled)
            AllowedRomanizationTags.Add(LanguageHelper.YueChineseLatnTag);
        if (settings.IsJapaneseRomanizationEnabled)
            AllowedRomanizationTags.Add(LanguageHelper.JapaneseLatnTag);
        if (settings.IsKoreanRomanizationEnabled)
            AllowedRomanizationTags.Add(LanguageHelper.KoreanLatnTag);
    }

    public bool IsRomanizationAllowed(LanguageTag? tag, LanguageTag? originalTag)
    {
        if (tag == null) return false;
        
        bool isAllowedBySettings = false;
        foreach (var allowed in AllowedRomanizationTags)
        {
            if (LanguageHelper.IsLanguageMatch(allowed, tag))
            {
                isAllowedBySettings = true;
                break;
            }
        }

        if (!isAllowedBySettings) return false;

        // 验证是否跟原文呈对应关系
        if (originalTag != null)
        {
            if (LanguageHelper.IsLanguageMatch(tag, LanguageHelper.MandarinChineseLatnTag))
                return LanguageHelper.IsLanguageMatch(originalTag, LanguageHelper.MandarinChineseCode);
            if (LanguageHelper.IsLanguageMatch(tag, LanguageHelper.YueChineseLatnTag))
                return LanguageHelper.IsLanguageMatch(originalTag, LanguageHelper.YueChineseCode);
            if (LanguageHelper.IsLanguageMatch(tag, LanguageHelper.JapaneseLatnTag))
                return LanguageHelper.IsLanguageMatch(originalTag, LanguageHelper.JapaneseCode);
            if (LanguageHelper.IsLanguageMatch(tag, LanguageHelper.KoreanLatnTag))
                return LanguageHelper.IsLanguageMatch(originalTag, LanguageHelper.KoreanCode);
        }

        return true;
    }

    public bool IsTranslationAllowed(LanguageTag? tag)
    {
        if (!IsTranslationEnabled) return false;
        if (tag == null) return true; // Accept translations without explicit tags as fallback
        return TargetTranslationTag == null || LanguageHelper.IsLanguageMatch(TargetTranslationTag, tag, true);
    }
}
