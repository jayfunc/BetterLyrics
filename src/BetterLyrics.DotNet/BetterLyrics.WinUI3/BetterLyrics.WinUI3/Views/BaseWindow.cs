using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Reflection;

namespace BetterLyrics.WinUI3.Views;

public class BaseWindow : Window
{
    private bool _isInitialized;
    private bool _isClosed;

    protected readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
    
    protected virtual bool EnableAppThemeChangeSync => true;

    public BaseWindow()
    {
        Activated += BaseWindow_Activated;
        Closed += BaseWindow_Closed;
    }

    private void BaseWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            Activated -= BaseWindow_Activated;

            if (EnableAppThemeChangeSync)
            {
                OnAppThemeChanged();
            }
            
            WeakReferenceMessenger.Default.RegisterAll(this);
            
            if (EnableAppThemeChangeSync)
            {
                WeakReferenceMessenger.Default.Register<AppThemeChangedMessage>(this, (r, m) =>
                {
                    ((BaseWindow)r).OnAppThemeChanged(m);
                });
            }
        }
    }

    protected virtual void OnAppThemeChanged(AppThemeChangedMessage? message = null)
    {
        this.SyncTheme();
    }

    public void BaseWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_isClosed) return;
        _isClosed = true;

        Activated -= BaseWindow_Activated;
        Closed -= BaseWindow_Closed;

        WeakReferenceMessenger.Default.UnregisterAll(this);
        _windowManagerProvider.OnWindowClosed(this);

        // 必须在 Content = null 之前收集，否则视觉树已被释放
        var (transientViewModels, transientViewModelHosts) = CollectTransientViewModels();

        Cleanup();

        Content = null;

        foreach (var viewModel in transientViewModels)
        {
            if (viewModel is ObservableRecipient recipient)
            {
                recipient.IsActive = false;
            }

            MemoryLeakDetector.Track(viewModel);
        }

        MemoryLeakDetector.Track(this);
        MemoryLeakDetector.ScheduleCheck();
    }

    /// <summary>
    /// 窗口自身持有、但不在视觉树中的 transient ViewModel（如窗口级 ViewModel）。
    /// </summary>
    protected virtual IEnumerable<object> GetAdditionalTrackedViewModels() => [];

    private (List<object> ViewModels, List<FrameworkElement> Hosts) CollectTransientViewModels()
    {
        var viewModels = new HashSet<object>();
        var hosts = new List<FrameworkElement>();

        if (Content is DependencyObject root)
        {
            CollectTransientViewModels(root, viewModels, hosts);
        }

        foreach (var viewModel in GetAdditionalTrackedViewModels())
        {
            if (viewModel is ITransientViewModel)
            {
                viewModels.Add(viewModel);
            }
        }

        return ([.. viewModels], hosts);
    }

    private static void CollectTransientViewModels(
        DependencyObject node, HashSet<object> viewModels, List<FrameworkElement> hosts)
    {
        if (node is FrameworkElement { DataContext: ITransientViewModel dataContextViewModel } element)
        {
            viewModels.Add(dataContextViewModel);
            hosts.Add(element);
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < childrenCount; i++)
        {
            CollectTransientViewModels(VisualTreeHelper.GetChild(node, i), viewModels, hosts);
        }
    }

    protected virtual void Cleanup() { }
}
