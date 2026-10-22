using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Messages.SettingsNavigation;

/// <summary>
/// 设置页内的选择发生变化时发送，由各设置控件 ViewModel 发出。
/// 消息携带自身路径信息，避免 SettingsPageViewModel 直接依赖各控件 ViewModel 实例。
/// </summary>
/// <param name="sender">发送方 ViewModel</param>
/// <param name="section">所属设置页一级分组</param>
/// <param name="subsection">当前二级分组</param>
/// <param name="parameter">路径参数（播放源 Provider 或歌词窗口状态）</param>
public sealed class SettingsSectionChangedMessage(
    object sender,
    SettingsSection section,
    Enum? subsection = null,
    object? parameter = null)
{
    public object Sender { get; } = sender;
    public SettingsSection Section { get; } = section;
    public Enum? Subsection { get; } = subsection;
    public object? Parameter { get; } = parameter;
}
