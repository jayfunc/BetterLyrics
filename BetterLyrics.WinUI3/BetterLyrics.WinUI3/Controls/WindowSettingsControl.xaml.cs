using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class WindowSettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(AlbumArtAreaEffectSettings), typeof(WindowSettingsControl), new PropertyMetadata(default));

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public ObservableCollection<string> MonitorDeviceNames { get; set; } = [];

    public WindowSettingsControl()
    {
        InitializeComponent();
        MonitorDeviceNames = [.. MonitorHook.GetAllMonitorDeviceNames()];
    }

    private void RefreshMonitorDeviceNames()
    {
        MonitorDeviceNames = [.. MonitorHook.GetAllMonitorDeviceNames()];
        LyricsWindowStatus.MonitorDeviceName = MonitorDeviceNames.FirstOrDefault() ?? "";
    }

    private void RefreshMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshMonitorDeviceNames();
    }
}
