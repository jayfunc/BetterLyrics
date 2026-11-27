using BetterLyrics.Core.Helpers.MediaFieldCorrection;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// 「插入示例」列表中的一项：直接展示图案原文。
/// </summary>
public sealed class MediaFieldCorrectionSnippetItemViewModel
{
    private readonly MediaFieldCorrectionSnippet _snippet;

    public string Pattern => _snippet.Pattern;

    public string Description { get; }

    public MediaFieldCorrectionSnippetItemViewModel(MediaFieldCorrectionSnippet snippet, string description)
    {
        _snippet = snippet;
        Description = description;
    }

    public MediaFieldMappingRule CreateRule() => _snippet.CreateRule();
}
