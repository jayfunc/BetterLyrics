using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class WindowSettingsControl : UserControl
{
    private readonly IMonitorProvider _monitorProvider =
        Ioc.Default.GetRequiredService<IMonitorProvider>();

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(WindowSettingsControl), new PropertyMetadata(default));

    public WindowSettingsControl()
    {
        InitializeComponent();
        MonitorDeviceNames = [.. _monitorProvider.GetAllMonitorDeviceNames()];
        Loaded += WindowSettingsControl_Loaded;
        Unloaded += WindowSettingsControl_Unloaded;
    }

    private void WindowSettingsControl_Loaded(object sender, RoutedEventArgs e)
    {
        Ioc.Default.GetRequiredService<IMonitorProvider>().MonitorsChanged += MonitorProvider_MonitorsChanged;
    }

    private void WindowSettingsControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Ioc.Default.GetRequiredService<IMonitorProvider>().MonitorsChanged -= MonitorProvider_MonitorsChanged;
    }

    private void MonitorProvider_MonitorsChanged(object? sender, System.EventArgs e)
    {
        // Wait a bit to ensure Windows display topology is fully updated
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(500);
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshMonitorDeviceNames();
            });
        });
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ObservableCollection<string> MonitorDeviceNames { get; set; } = [];

    private void RefreshMonitorDeviceNames()
    {
        var currentNames = _monitorProvider.GetAllMonitorDeviceNames().ToList();
        
        MonitorDeviceNames.Clear();
        foreach (var name in currentNames)
        {
            MonitorDeviceNames.Add(name);
        }

        if (LyricsWindowStatus != null && !currentNames.Contains(LyricsWindowStatus.MonitorDeviceName))
        {
            LyricsWindowStatus.MonitorDeviceName = currentNames.FirstOrDefault() ?? "";
        }
    }

    private void RefreshMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshMonitorDeviceNames();
    }
}