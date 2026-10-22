using BetterLyrics.WinUI3.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// 带有内存泄漏检测的 UserControl 基类。
/// 卸载时跟踪自身及子树中的 Transient ViewModel，并触发一次 GC 检查。
/// </summary>
public partial class BaseUserControl : UserControl
{
    protected BaseUserControl()
    {
        Unloaded += BaseUserControl_Unloaded;
    }

    protected virtual void Cleanup() { }

    private void BaseUserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Cleanup();

        var viewModels = ViewMemoryLeakTracker.CollectTransientViewModels(this);
        ViewMemoryLeakTracker.TrackAndCheck(viewModels, this);
    }
}
