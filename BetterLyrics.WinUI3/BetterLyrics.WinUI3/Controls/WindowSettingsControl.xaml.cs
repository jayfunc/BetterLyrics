using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
