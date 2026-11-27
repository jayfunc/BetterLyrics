namespace BetterLyrics.Core.Models.Settings;

/// <summary>
/// 用于导入 / 导出的规则包，方便用户之间互相分享「热修复」规则。
/// </summary>
public class MediaFieldRulePackage
{
    /// <summary>
    /// 规则原本所属的播放器（仅供查看，导入时以当前播放器为准）。
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    public List<MediaFieldMappingRule> Rules { get; set; } = [];
}
