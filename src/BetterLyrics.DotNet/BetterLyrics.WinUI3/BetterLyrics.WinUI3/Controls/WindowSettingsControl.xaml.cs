using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class WindowSettingsControl : UserControl
{
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(WindowSettingsControl), new PropertyMetadata(default));

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ObservableCollection<string> MonitorDeviceNames { get; set; } = [];

    public WindowSettingsControl()
    {
        InitializeComponent();
        MonitorDeviceNames = [.. _monitorProvider.GetAllMonitorDeviceNames()];
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
         _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;
    }

    private void MonitorProvider_MonitorsChanged(object? sender, System.EventArgs e)
    {
        // Wait a bit to ensure Windows display topology is fully updated
        Task.Run(async () =>
        {
            await Task.Delay(500);
            DispatcherQueue.TryEnqueue(() =>
            {
                RefreshMonitorDeviceNames();
            });
        });
    }

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