using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class DemoWindowGrid : UserControl
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(DemoWindowGrid),
            new PropertyMetadata(default, OnLyricsWindowStatusChanged));

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();

    private LyricsWindowStatus? _subscribedStatus;

    public DemoWindowGrid()
    {
        InitializeComponent();
    }

    private static void OnLyricsWindowStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DemoWindowGrid grid)
        {
            // The same LyricsWindowStatus instance is reused by the settings UI, so the
            // monitor can change without this dependency property ever changing value.
            grid.DetachStatus();
            grid.AttachStatus(e.NewValue as LyricsWindowStatus);
            grid.UpdateMonitorWarningVisibility();
        }
    }

    private void AttachStatus(LyricsWindowStatus? status)
    {
        if (status == null || ReferenceEquals(_subscribedStatus, status)) return;

        _subscribedStatus = status;
        _subscribedStatus.PropertyChanged += Status_PropertyChanged;
    }

    private void DetachStatus()
    {
        if (_subscribedStatus == null) return;

        _subscribedStatus.PropertyChanged -= Status_PropertyChanged;
        _subscribedStatus = null;
    }

    private void Status_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(LyricsWindowStatus.MonitorDeviceName))
        {
            UpdateMonitorWarningVisibility();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        AttachStatus(LyricsWindowStatus);
        _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;
        UpdateMonitorWarningVisibility();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachStatus();
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;
    }

    private void MonitorProvider_MonitorsChanged(object? sender, System.EventArgs e)
    {
        var dispatcherQueue = DispatcherQueue;
        if (dispatcherQueue == null) return;

        _ = Task.Run(async () =>
        {
            // Give the OS a moment to settle the display topology before re-checking.
            await Task.Delay(500);
            dispatcherQueue.TryEnqueue(UpdateMonitorWarningVisibility);
        });
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var data = (LyricsWindowStatus)((FrameworkElement)sender).DataContext;
        var window = _windowManagerProvider.GetNowPlayingWindow(data);
        if (window != null) _windowManagerProvider.CloseWindow(window);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var status = (LyricsWindowStatus)((FrameworkElement)sender).DataContext;
        // 多窗口模式
        if (_settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
        {
            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
        }
        // 单窗口模式
        else
        {
            var openedWindows = _windowManagerProvider.GetWindows<NowPlayingWindow>();
            foreach (var item in openedWindows.Where(x => x.LyricsWindowStatus != status))
            {
                _windowManagerProvider.CloseWindow(item);
            }

            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
        }
    }

    public void UpdateMonitorWarningVisibility()
    {
        if (DisconnectedWarningGrid == null) return;

        var dispatcherQueue = DispatcherQueue;
        if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
        {
            dispatcherQueue.TryEnqueue(UpdateMonitorWarningVisibility);
            return;
        }

        var status = LyricsWindowStatus;
        if (status == null || string.IsNullOrEmpty(status.MonitorDeviceName))
        {
            DisconnectedWarningGrid.Visibility = Visibility.Collapsed;
            return;
        }

        var isConnected = _monitorProvider.GetAllMonitorDeviceNames()
            .Contains(status.MonitorDeviceName, StringComparer.OrdinalIgnoreCase);

        DisconnectedWarningGrid.Visibility = isConnected ? Visibility.Collapsed : Visibility.Visible;
    }
}
