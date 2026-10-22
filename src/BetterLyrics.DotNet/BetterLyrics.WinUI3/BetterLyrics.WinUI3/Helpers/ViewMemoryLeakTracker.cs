using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Helpers;

/// <summary>
/// 视图侧（Window / Page / UserControl）统一的内存泄漏跟踪入口。
/// <para>
/// ViewModel 没有卸载或销毁事件，且并非都继承 ObservableRecipient，
/// 无法可靠地“自身触发”泄漏检测，因此统一由视图在关闭/卸载时收集并跟踪。
/// </para>
/// </summary>
public static class ViewMemoryLeakTracker
{
    /// <summary>
    /// 收集视觉树中的 Transient ViewModel（DataContext 实现 <see cref="ITransientViewModel"/>）。
    /// 需要在清理视觉树之前调用。
    /// </summary>
    public static List<object> CollectTransientViewModels(
        DependencyObject? root, IEnumerable<object>? additionalViewModels = null)
    {
        var viewModels = new HashSet<object>();

        if (root is not null)
        {
            CollectTransientViewModels(root, viewModels);
        }

        if (additionalViewModels is not null)
        {
            foreach (var viewModel in additionalViewModels)
            {
                if (viewModel is ITransientViewModel)
                {
                    viewModels.Add(viewModel);
                }
            }
        }

        return [.. viewModels];
    }

    /// <summary>
    /// 跟踪已收集的 ViewModel 与视图自身，并调度一次 GC 检查。
    /// </summary>
    public static void TrackAndCheck(IEnumerable<object> viewModels, object self)
    {
        foreach (var viewModel in viewModels)
        {
            MemoryLeakDetector.Track(viewModel);
        }

        MemoryLeakDetector.Track(self);
        MemoryLeakDetector.ScheduleCheck();
    }

    private static void CollectTransientViewModels(DependencyObject node, HashSet<object> viewModels)
    {
        if (node is FrameworkElement { DataContext: ITransientViewModel dataContextViewModel })
        {
            viewModels.Add(dataContextViewModel);
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < childrenCount; i++)
        {
            CollectTransientViewModels(VisualTreeHelper.GetChild(node, i), viewModels);
        }
    }
}
