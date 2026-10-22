using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
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
        var transientViewModels = ViewMemoryLeakTracker.CollectTransientViewModels(
            Content as DependencyObject, GetAdditionalTrackedViewModels());

        Cleanup();

        Content = null;

        foreach (var viewModel in transientViewModels)
        {
            if (viewModel is ObservableRecipient recipient)
            {
                recipient.IsActive = false;
            }
        }

        ViewMemoryLeakTracker.TrackAndCheck(transientViewModels, this);
    }

    /// <summary>
    /// 窗口自身持有、但不在视觉树中的 transient ViewModel（如窗口级 ViewModel）。
    /// </summary>
    protected virtual IEnumerable<object> GetAdditionalTrackedViewModels() => [];

    protected virtual void Cleanup() { }
}
