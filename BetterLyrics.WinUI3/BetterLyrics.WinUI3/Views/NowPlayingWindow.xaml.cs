// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics.Imaging;
using Windows.UI;
using WinRT.Interop;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingWindow : Window,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<BitmapDecoder?>>
    {
        private ForegroundWindowHook? _fgWindowWatcher = null;
        private DispatcherQueueTimer? _fgWindowWatcherTimer = null;

        private Color _backdropAccentColor = Colors.Transparent;

        private List<Color> _lightAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();
        private List<Color> _darkAccentColors = Enumerable.Repeat(Colors.Black, 4).ToList();

        public LyricsWindowStatus Status { get; private set; }

        public NowPlayingWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<NowPlayingWindowViewModel>();
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public NowPlayingWindow(LyricsWindowStatus status)
        {
            this.InitializeComponent();

            _fgWindowWatcherTimer = DispatcherQueue.CreateTimer();

            Status = status;
            NowPlayingPage.LyricsWindowStatus = Status;

            this.Init("LyricsPageTitle", TitleBarHeightOption.Collapsed, BackdropType.Transparent);

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<BitmapDecoder?>>(this);

            _ = UpdateAlbumArtThemeColorsAsync();
        }

        public async Task InitStatus()
        {
            Status.PropertyChanged += LyricsWindowStatus_PropertyChanged;

            Status.IsLyricsWindowStatusRefreshing = true;

            Status.UpdateMonitorBounds();

            this.SetIsWorkArea(Status.IsWorkArea);
            if (Status.IsWorkArea)
            {
                this.UpdateWorkArea();
            }
            await Task.Delay(300);

            this.SetIsShowInSwitchers(Status.IsShownInSwitchers);
            this.SetIsAlwaysOnTop(Status.IsAlwaysOnTop);

            this.SetIsClickThrough(Status.IsClickThrough);
            this.SetIsBorderless(Status.IsBorderless);

            this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
            this.SetTitleBarArea(Status.TitleBarArea);

            // 下述代码可以删除，但是为了避免给用户造成操作上的疑虑，暂时保留
            if (Status.IsWorkArea)
            {
                Status.WindowBounds = Status.GetWindowBoundsWhenWorkArea();
            }

            this.MoveAndResize(Status.WindowBounds);
            Status.WindowX = Status.WindowBounds.X;
            Status.WindowY = Status.WindowBounds.Y;
            Status.WindowWidth = Status.WindowBounds.Width;
            Status.WindowHeight = Status.WindowBounds.Height;

            Status.UpdateDemoWindowAndMonitorBounds();

            Status.IsLyricsWindowStatusRefreshing = false;
        }

        private async void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LyricsWindowStatus.IsWorkArea):
                    Status.IsLyricsWindowStatusRefreshing = true;
                    this.SetIsWorkArea(Status.IsWorkArea);
                    Status.IsLyricsWindowStatusRefreshing = false;
                    if (Status.IsWorkArea)
                    {
                        this.MoveAndResize(Status.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.DockHeight):
                case nameof(LyricsWindowStatus.DockPlacement):
                case nameof(LyricsWindowStatus.MonitorDeviceName):
                    Status.UpdateMonitorBounds();
                    if (Status.IsWorkArea)
                    {
                        Status.IsLyricsWindowStatusRefreshing = true;
                        this.UpdateWorkArea();
                        Status.IsLyricsWindowStatusRefreshing = false;
                        this.MoveAndResize(Status.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.IsShownInSwitchers):
                    this.SetIsShowInSwitchers(Status.IsShownInSwitchers);
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysOnTop):
                    this.SetIsAlwaysOnTop(Status.IsAlwaysOnTop);
                    break;
                case nameof(LyricsWindowStatus.IsClickThrough):
                    this.SetIsClickThrough(Status.IsClickThrough);
                    break;
                case nameof(LyricsWindowStatus.IsBorderless):
                    this.SetIsBorderless(Status.IsBorderless);
                    break;
                case nameof(LyricsWindowStatus.WindowX):
                    this.MoveAndResize(Status.WindowBounds.WithX(Status.WindowX));
                    break;
                case nameof(LyricsWindowStatus.WindowY):
                    this.MoveAndResize(Status.WindowBounds.WithY(Status.WindowY));
                    break;
                case nameof(LyricsWindowStatus.WindowWidth):
                    this.MoveAndResize(Status.WindowBounds.WithWidth(Status.WindowWidth));
                    break;
                case nameof(LyricsWindowStatus.WindowHeight):
                    this.MoveAndResize(Status.WindowBounds.WithHeight(Status.WindowHeight));
                    break;
                case nameof(LyricsWindowStatus.TitleBarArea):
                    this.SetTitleBarArea(Status.TitleBarArea);
                    break;
                case nameof(LyricsWindowStatus.AutoShowOrHideWindow):
                    this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
                    break;
                case nameof(LyricsWindowStatus.LyricsBackgroundSettings):
                    await UpdateAlbumArtThemeColorsAsync();
                    break;
                default:
                    break;
            }
        }

        public void UpdateBackdropAccentColor(nint hwnd)
        {
            _backdropAccentColor = Helper.ColorHelper.GetAccentColor(
                hwnd,
                Status.MonitorDeviceName,
                Status.EnvironmentSampleMode);
            UpdateAlbumArtThemeColorsAsync();
        }

        public void InitFgWindowWatcher()
        {
            var hwnd = WindowNative.GetWindowHandle(this);

            _fgWindowWatcher = new ForegroundWindowHook(
                hwnd,
                fgHwnd =>
                {
                    _fgWindowWatcherTimer?.Debounce(() =>
                    {
                        if (Status.IsAlwaysOnTop &&
                            Status.IsAlwaysOnTopPolling &&
                            this.AppWindow != null &&
                            this.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsAlwaysOnTop = true;
                        }
                        if (Status.IsAdaptToEnvironment)
                        {
                            UpdateBackdropAccentColor(hwnd);
                        }
                    }, Constants.Time.DebounceTimeout);
                }
            );
            _fgWindowWatcher.Start();
            UpdateBackdropAccentColor(hwnd);
        }

        private async Task UpdateAlbumArtThemeColorsAsync()
        {
            if (_mediaSessionsService.AlbumArtBitmapDecoder is BitmapDecoder decoder)
            {
                var lightPalette = await ImageHelper.GetAccentColorsAsync(_mediaSessionsService.AlbumArtBitmapDecoder, 4, Status.LyricsBackgroundSettings.PaletteGeneratorType, false);
                var darkPalette = await ImageHelper.GetAccentColorsAsync(_mediaSessionsService.AlbumArtBitmapDecoder, 4, Status.LyricsBackgroundSettings.PaletteGeneratorType, true);
                _lightAccentColors = lightPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
                _darkAccentColors = darkPalette.Palette.Select(Helper.ColorHelper.FromVector3).ToList();
            }

            var result = new AlbumArtThemeColors();
            result.EnvColor = _backdropAccentColor;

            ElementTheme themeTypeSent;
            if (Status.IsAdaptToEnvironment)
            {
                themeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(result.EnvColor);
            }
            else
            {
                themeTypeSent = Status.LyricsBackgroundSettings.LyricsBackgroundTheme;
            }

            bool isLight = themeTypeSent switch
            {
                ElementTheme.Default => Application.Current.RequestedTheme == ApplicationTheme.Light,
                ElementTheme.Light => true,
                ElementTheme.Dark => false,
                _ => false
            };

            Color adaptiveGrayedFontColor;
            Color grayedEnvironmentalColor;
            Color? adaptiveColoredFontColor;

            Color darkColor = Colors.Black;
            Color lightColor = Colors.White;

            if (isLight)
            {
                adaptiveGrayedFontColor = darkColor;
                // brightness = 0.7f;
                grayedEnvironmentalColor = lightColor;

                result.AccentColor1 = _lightAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _lightAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _lightAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _lightAccentColors.ElementAtOrDefault(3);
            }
            else
            {
                adaptiveGrayedFontColor = lightColor;
                // brightness = 0.3f;
                grayedEnvironmentalColor = darkColor;

                result.AccentColor1 = _darkAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _darkAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _darkAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _darkAccentColors.ElementAtOrDefault(3);
            }

            if (Status.IsAdaptToEnvironment)
            {
                adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(result.EnvColor);
            }
            else
            {
                if (isLight)
                    adaptiveColoredFontColor = _darkAccentColors.ElementAtOrDefault(0);
                else
                    adaptiveColoredFontColor = _lightAccentColors.ElementAtOrDefault(0);
            }

            result.ThemeType = themeTypeSent;

            // 背景字色
            switch (Status.LyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.BgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.BgFontColor = Status.LyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 前景字色
            switch (Status.LyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.FgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.FgFontColor = Status.LyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 描边颜色
            switch (Status.LyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.StrokeFontColor = grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.StrokeFontColor = result.EnvColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    result.StrokeFontColor = Status.LyricsStyleSettings.LyricsCustomStrokeFontColor;
                    break;
                default:
                    result.StrokeFontColor = Colors.Transparent;
                    break;
            }

            NowPlayingPage.AlbumArtThemeColors = result;
            RootGrid.RequestedTheme = result.ThemeType;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            ExitOrClose();
            args.Cancel = true;
        }

        private void ExitOrClose()
        {
            _fgWindowWatcherTimer = null;
            _fgWindowWatcher?.Stop();
            _fgWindowWatcher = null;
            if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.CloseWindow();
            }
        }

        public void SetTitleBarArea(TitleBarArea titleBarArea)
        {
            switch (titleBarArea)
            {
                case TitleBarArea.None:
                    SetTitleBar(PlaceholderGrid);
                    break;
                case TitleBarArea.Top:
                    SetTitleBar(TopCommandGrid);
                    break;
                case TitleBarArea.Whole:
                    SetTitleBar(RootGrid);
                    break;
                default:
                    break;
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (Status.IsLyricsWindowStatusRefreshing)
            {
                return;
            }

            if (args.DidPositionChange || args.DidSizeChange)
            {
                var size = AppWindow.Size;
                var rect = AppWindow.Position;

                if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
                {
                    return;
                }
                else
                {
                    Status.WindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
                }
            }
        }

        private void TopCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.TopCommandGridOpacity = 1f;
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.TopCommandGridOpacity = 0f;
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ExitOrClose();
        }

        private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        private void SettingsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.MinimizeWindow();
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentIsPlaying))
                {
                    this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
                }
            }
        }

        public async void Receive(PropertyChangedMessage<BitmapDecoder?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapDecoder))
                {
                    if (message.NewValue is BitmapDecoder decoder)
                    {
                        await UpdateAlbumArtThemeColorsAsync();
                    }
                }
            }
        }

    }
}
