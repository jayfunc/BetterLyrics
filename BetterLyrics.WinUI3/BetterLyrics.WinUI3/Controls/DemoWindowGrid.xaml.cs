using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class DemoWindowGrid : UserControl
{
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    public DemoWindowGrid()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(DemoWindowGrid), new PropertyMetadata(default));

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var data = (LyricsWindowStatus)(((FrameworkElement)sender).DataContext);
        var window = WindowHook.GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == data);
        window?.CloseWindow();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var status = (LyricsWindowStatus)(((FrameworkElement)sender).DataContext);
        // 多开模式
        if (_settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
        {
            WindowHook.OpenOrShowWindow<NowPlayingWindow>(status);
        }
        // 单例模式
        else
        {
            var openedWindows = WindowHook.GetWindows<NowPlayingWindow>();
            foreach (var item in openedWindows.Where(x => x.LyricsWindowStatus != status))
            {
                item.CloseWindow();
            }
            WindowHook.OpenOrShowWindow<NowPlayingWindow>(status);
        }
    }

}
