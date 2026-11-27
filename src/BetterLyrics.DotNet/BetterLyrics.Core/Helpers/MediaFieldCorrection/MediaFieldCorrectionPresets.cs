using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers.MediaFieldCorrection;

/// <summary>
/// 可一键插入的规则示例。列表里直接显示图案本身，方便按需修改。
/// </summary>
public sealed class MediaFieldCorrectionSnippet
{
    public required MediaFieldRuleKind Kind { get; init; }

    public required MediaField Source { get; init; }

    /// <summary>
    /// 模板或正则原文，同时作为菜单里显示的标题。
    /// </summary>
    public required string Pattern { get; init; }

    public string Replacement { get; init; } = string.Empty;

    public required string DescriptionKey { get; init; }

    public MediaFieldMappingRule CreateRule() => new()
    {
        Kind = Kind,
        Source = Source,
        Pattern = Pattern,
        Replacement = Replacement
    };
}

public static class MediaFieldCorrectionPresets
{
    public static IReadOnlyList<MediaFieldCorrectionSnippet> Snippets { get; } =
    [
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = "{artist} - {title}",
            DescriptionKey = "MediaFieldSnippetTitleArtistDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = "{title} - {artist}",
            DescriptionKey = "MediaFieldSnippetTitleArtistReversedDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Artist,
            Pattern = "{artist} — {album}",
            DescriptionKey = "MediaFieldSnippetArtistAlbumDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = "{title} - {ignore}",
            DescriptionKey = "MediaFieldSnippetTrimDashSuffixDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = "{title} ({ignore})*",
            DescriptionKey = "MediaFieldSnippetRemoveBracketsDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.Template,
            Source = MediaField.Title,
            Pattern = "{title} feat. {ignore}",
            DescriptionKey = "MediaFieldSnippetRemoveFeatDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.RegexExtract,
            Source = MediaField.Title,
            Pattern = @"^(?<title>.+?)\s*[\(\[【]",
            DescriptionKey = "MediaFieldSnippetRegexExtractBracketDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.RegexReplace,
            Source = MediaField.Album,
            Pattern = @"\s*[-–—]\s*(?:Single|EP)$",
            DescriptionKey = "MediaFieldSnippetRegexRemoveAlbumSuffixDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.RegexReplace,
            Source = MediaField.Title,
            Pattern = @"\s+",
            Replacement = " ",
            DescriptionKey = "MediaFieldSnippetRegexCollapseSpacesDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.RegexReplace,
            Source = MediaField.Artist,
            Pattern = @"\s*/\s*",
            Replacement = ", ",
            DescriptionKey = "MediaFieldSnippetRegexArtistSeparatorDescription"
        },
        new()
        {
            Kind = MediaFieldRuleKind.RegexReplace,
            Source = MediaField.Title,
            Pattern = @"^\s*\d{1,3}\s*[\.\)\]、-]\s*",
            DescriptionKey = "MediaFieldSnippetRegexTrackNumberDescription"
        }
    ];

    /// <summary>
    /// Apple Music 会把「歌手 — 专辑」整体塞进歌手字段。
    /// 这里用两条可读的规则表达原来硬编码的逻辑，用户可以直接改。
    /// </summary>
    public static IReadOnlyList<MediaFieldMappingRule> CreateAppleMusicRules()
    {
        return
        [
            new MediaFieldMappingRule
            {
                Kind = MediaFieldRuleKind.Template,
                Source = MediaField.Artist,
                Pattern = "{artist} — {album}"
            },
            new MediaFieldMappingRule
            {
                Kind = MediaFieldRuleKind.RegexReplace,
                Source = MediaField.Album,
                Pattern = @"\s*[-–—]\s*(?:Single|EP)$"
            }
        ];
    }
}
