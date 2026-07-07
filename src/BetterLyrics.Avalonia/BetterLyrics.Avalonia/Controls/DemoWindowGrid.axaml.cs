using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class DemoWindowGrid : UserControl
{
    public static readonly StyledProperty<LyricsWindowStatus> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<DemoWindowGrid, LyricsWindowStatus>(nameof(LyricsWindowStatus));

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public DemoWindowGrid()
    {
        InitializeComponent();
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        var status = LyricsWindowStatus;
        if (status == null) return;

        var window = _windowManagerProvider.GetNowPlayingWindow(status);
        if (window != null) _windowManagerProvider.CloseWindow(window);
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        var status = LyricsWindowStatus;
        if (status == null) return;

        // 多窗口模式
        if (_settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
        {
            _windowManagerProvider.OpenOrShowWindow(WindowType.NowPlayingWindow, status);
        }
        // 单窗口模式
        else
        {
            var openedWindows = _windowManagerProvider.GetWindows(WindowType.NowPlayingWindow);
            var targetWindow = _windowManagerProvider.GetWindow(WindowType.NowPlayingWindow, status);
            foreach (var item in openedWindows.Where(x => x != targetWindow))
                _windowManagerProvider.CloseWindow(item);

            _windowManagerProvider.OpenOrShowWindow(WindowType.NowPlayingWindow, status);
        }
    }
}