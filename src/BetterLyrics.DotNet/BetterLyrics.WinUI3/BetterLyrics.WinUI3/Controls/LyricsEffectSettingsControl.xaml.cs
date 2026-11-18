using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsEffectSettingsControl : UserControl
{
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();
    private DisplayInformation? _display;
    private LyricsWindowStatus? _observedStatus;

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(LyricsEffectSettingsControl), new PropertyMetadata(null, OnLyricsWindowStatusChanged));

    public static readonly DependencyProperty LyricsEffectSettingsProperty =
        DependencyProperty.Register(nameof(LyricsEffectSettings), typeof(LyricsEffectSettings),
            typeof(LyricsEffectSettingsControl), new PropertyMetadata(default));

    public LyricsEffectSettings LyricsEffectSettings
    {
        get => (LyricsEffectSettings)GetValue(LyricsEffectSettingsProperty);
        set => SetValue(LyricsEffectSettingsProperty, value);
    }

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public LyricsEffectSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnLyricsWindowStatusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (LyricsEffectSettingsControl)sender;
        if (control.IsLoaded) control.ObserveWindow(control.LyricsWindowStatus);
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        _monitorProvider.MonitorsChanged += OnMonitorsChanged;
        ObserveWindow(LyricsWindowStatus);
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _monitorProvider.MonitorsChanged -= OnMonitorsChanged;
        ObserveWindow(null);
    }

    private void ObserveWindow(LyricsWindowStatus? status)
    {
        if (_observedStatus != null) _observedStatus.PropertyChanged -= OnWindowChanged;
        _observedStatus = status;
        if (_observedStatus != null) _observedStatus.PropertyChanged += OnWindowChanged;
        ObserveDisplay();
    }

    private void OnWindowChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(LyricsWindowStatus.MonitorBounds) or
            nameof(LyricsWindowStatus.MonitorDevicePath) or nameof(LyricsWindowStatus.MonitorDeviceName))
        {
            DispatcherQueue.TryEnqueue(ObserveDisplay);
        }
    }

    private void OnMonitorsChanged(object? sender, EventArgs args) => DispatcherQueue.TryEnqueue(ObserveDisplay);

    private void ObserveDisplay()
    {
        if (_display != null) _display.AdvancedColorInfoChanged -= OnAdvancedColorChanged;
        _display = null;
        HdrGlowToggle.IsEnabled = false;

        if (_observedStatus is not { } status) return;
        var monitor = _monitorProvider.ResolveMonitor(status.MonitorDevicePath, status.MonitorEdid,
            status.MonitorDeviceName, status.MonitorBounds);
        if (monitor is not { IsActive: true }) return;

        var bounds = _monitorProvider.GetMonitorRectFromDeviceName(monitor.GdiDeviceName);
        if (bounds.IsEmpty) return;
        var center = new POINT((int)(bounds.X + bounds.Width / 2), (int)(bounds.Y + bounds.Height / 2));
        var handle = User32.MonitorFromPoint(center, User32.MonitorFlags.MONITOR_DEFAULTTONULL);
        if (handle == default) return;

        try
        {
            _display = DisplayInformation.CreateForDisplayId(Win32Interop.GetDisplayIdFromMonitor((IntPtr)handle));
            _display.AdvancedColorInfoChanged += OnAdvancedColorChanged;
            UpdateHdrToggle();
        }
        catch (Exception error) when (error is COMException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            Debug.WriteLine(error);
        }
    }

    private void OnAdvancedColorChanged(DisplayInformation sender, object args) =>
        DispatcherQueue.TryEnqueue(UpdateHdrToggle);

    private void UpdateHdrToggle()
    {
        try
        {
            HdrGlowToggle.IsEnabled =
                _display?.GetAdvancedColorInfo().CurrentAdvancedColorKind == DisplayAdvancedColorKind.HighDynamicRange;
        }
        catch (Exception error) when (error is COMException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            HdrGlowToggle.IsEnabled = false;
            Debug.WriteLine(error);
        }
    }
}
