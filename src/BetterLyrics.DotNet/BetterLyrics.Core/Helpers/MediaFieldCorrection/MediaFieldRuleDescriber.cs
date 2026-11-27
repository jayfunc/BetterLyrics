using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers.MediaFieldCorrection;

/// <summary>
/// 把一条规则概括成一句话，供规则列表展示。
/// </summary>
public static class MediaFieldRuleDescriber
{
    public static string Describe(MediaFieldMappingRule rule, ILocalizationService localizationService)
    {
        var source = GetFieldName(rule.Source, localizationService);

        return rule.Kind switch
        {
            MediaFieldRuleKind.Template => Format(localizationService,
                "MediaFieldRuleSummaryTemplate", source, rule.Pattern),

            MediaFieldRuleKind.RegexExtract => Format(localizationService,
                "MediaFieldRuleSummaryRegexExtract", source, rule.Pattern),

            MediaFieldRuleKind.RegexReplace => Format(localizationService,
                "MediaFieldRuleSummaryRegexReplace", source, rule.Pattern,
                string.IsNullOrEmpty(rule.Replacement) ? "\"\"" : rule.Replacement),

            _ => string.Empty
        };
    }

    public static string GetFieldName(MediaField field, ILocalizationService localizationService)
    {
        var key = field switch
        {
            MediaField.Title => "MediaFieldTitle/Content",
            MediaField.Artist => "MediaFieldArtist/Content",
            MediaField.Album => "MediaFieldAlbum/Content",
            _ => string.Empty
        };

        var value = localizationService.GetLocalizedString(key);
        return string.IsNullOrEmpty(value) ? field.ToString() : value;
    }

    private static string Format(ILocalizationService localizationService, string key, params object[] args)
    {
        var template = localizationService.GetLocalizedString(key);
        if (string.IsNullOrEmpty(template)) return key;

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
