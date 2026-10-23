using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

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

        MemoryLeakDetector.Track(this);

        foreach (var item in GetAdditionalTrackedViewModels())
        {
            MemoryLeakDetector.Track(item);
        }

        Cleanup();

        Content = null;

        MemoryLeakDetector.ScheduleCheck();
    }

    protected virtual IEnumerable<object> GetAdditionalTrackedViewModels() => [];

    protected virtual void Cleanup() { }
}
