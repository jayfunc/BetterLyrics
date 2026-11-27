namespace BetterLyrics.Core.Enums;

/// <summary>
/// 播放器通过 SMTC 上报、可以参与纠正的歌曲信息字段。
/// 枚举顺序同时被界面的下拉框使用，请勿随意调整。
/// </summary>
public enum MediaField
{
    Title = 0,
    Artist = 1,
    Album = 2
}
