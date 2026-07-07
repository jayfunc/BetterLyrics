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
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ObservableCollection<string> MonitorDeviceNames { get; set; } = [];

    private void RefreshMonitorDeviceNames()
    {
        MonitorDeviceNames = [.. _monitorProvider.GetAllMonitorDeviceNames()];
        LyricsWindowStatus.MonitorDeviceName = MonitorDeviceNames.FirstOrDefault() ?? "";
    }

    private void RefreshMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshMonitorDeviceNames();
    }
}