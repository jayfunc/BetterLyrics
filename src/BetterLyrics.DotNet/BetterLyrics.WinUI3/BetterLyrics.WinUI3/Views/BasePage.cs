using BetterLyrics.WinUI3.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// 带有内存泄漏检测的 Page 基类。
/// 卸载时执行清理、跟踪自身及子树中的 Transient ViewModel，并触发一次 GC 检查。
/// </summary>
public partial class BasePage : Page
{
    protected BasePage()
    {
        Unloaded += BasePage_Unloaded;
    }

    public virtual void Cleanup() { }

    private void BasePage_Unloaded(object sender, RoutedEventArgs e)
    {
        // 先收集，避免 Cleanup 中清空 DataContext 后丢失对 ViewModel 的引用
        var viewModels = ViewMemoryLeakTracker.CollectTransientViewModels(this);

        Cleanup();

        ViewMemoryLeakTracker.TrackAndCheck(viewModels, this);
    }
}
