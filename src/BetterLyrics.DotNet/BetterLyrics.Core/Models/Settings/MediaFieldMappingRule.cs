using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

/// <summary>
/// 播放器字段纠正规则。
/// 规则按列表顺序执行，后一条规则可以读取前面规则写入的结果。
///
/// <see cref="MediaFieldRuleKind.Template"/>：
///     Pattern = "{artist} - {title}"，{title}/{artist}/{album} 会写回对应字段，
///     其他名字（如 {ignore}）表示丢弃该部分，* 表示任意剩余内容。
/// <see cref="MediaFieldRuleKind.RegexExtract"/>：
///     Pattern = "^(?&lt;artist&gt;.+?) - (?&lt;title&gt;.+)$"，命名分组写回对应字段。
/// <see cref="MediaFieldRuleKind.RegexReplace"/>：
///     Pattern = @"\s*-\s*(Single|EP)$"，Replacement = ""，对整个字段做替换。
/// </summary>
public partial class MediaFieldMappingRule : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial MediaFieldRuleKind Kind { get; set; } = MediaFieldRuleKind.Template;

    /// <summary>
    /// 规则读取并写回的字段。
    /// </summary>
    [ObservableProperty]
    public partial MediaField Source { get; set; } = MediaField.Title;

    /// <summary>
    /// 模板或正则。
    /// </summary>
    [ObservableProperty]
    public partial string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// 仅 <see cref="MediaFieldRuleKind.RegexReplace"/> 使用；留空表示删除匹配到的内容。
    /// </summary>
    [ObservableProperty]
    public partial string Replacement { get; set; } = string.Empty;

    public MediaFieldMappingRule Clone()
    {
        return new MediaFieldMappingRule
        {
            IsEnabled = IsEnabled,
            Kind = Kind,
            Source = Source,
            Pattern = Pattern,
            Replacement = Replacement
        };
    }

    public void CopyFrom(MediaFieldMappingRule other)
    {
        IsEnabled = other.IsEnabled;
        Kind = other.Kind;
        Source = other.Source;
        Pattern = other.Pattern;
        Replacement = other.Replacement;
    }
}
