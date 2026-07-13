using System.Linq;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class DemoWindowGrid : UserControl
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(DemoWindowGrid),
            new PropertyMetadata(default));

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly IWindowManagerProvider
        _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public DemoWindowGrid()
    {
        InitializeComponent();
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
        // �࿪ģʽ
        if (_settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
        {
            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
        }
        // ����ģʽ
        else
        {
            var openedWindows = _windowManagerProvider.GetWindows<NowPlayingWindow>();
            foreach (var item in openedWindows.Where(x => x.LyricsWindowStatus != status))
                _windowManagerProvider.CloseWindow(item);

            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
        }
    }
}