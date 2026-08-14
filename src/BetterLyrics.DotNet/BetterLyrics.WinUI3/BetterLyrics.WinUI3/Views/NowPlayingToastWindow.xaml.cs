using System;
using System.IO;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;
using Vanara.PInvoke;
using WinRT.Interop;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Helpers;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class NowPlayingToastWindow : Window,
    IRecipient<PropertyChangedMessage<AppTheme>>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private readonly IGsmtcService _gsmtcService =
        Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly DispatcherTimer _hideTimer;
    private bool _isShowing;

    public NowPlayingToastWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.Init("NowPlayingToastTitle", titleBarHeightOption: TitleBarHeightOption.Collapsed,
            backdropType: BackdropType.Transparent);
        _windowManagerProvider.SetIsBorderless(this, true);
        AppWindow.IsShownInSwitchers = false;
        _windowManagerProvider.SetIsClickThrough(this, true);
        SyncNowPlayingTheme();

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3.5)
        };
        _hideTimer.Tick += HideTimer_Tick;
    }

    private void PopupContainer_Loaded(object sender, RoutedEventArgs e)
    {
        SharedShadow.Receivers.Add(ShadowCastGrid);
        PopupContainer.Translation = new System.Numerics.Vector3(0, 0, 32);
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.AppTheme) || message.PropertyName == nameof(GeneralSettings.NowPlayingNotificationTheme))
                SyncNowPlayingTheme();
    }

    private void SyncNowPlayingTheme()
    {
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        if (settingsService == null || this.Content == null) return;

        var theme = settingsService.AppSettings.GeneralSettings.NowPlayingNotificationTheme;
        if (theme == AppTheme.Default)
            theme = settingsService.AppSettings.GeneralSettings.AppTheme;

        this.AppWindow.TitleBar.PreferredTheme = theme.ToTitleBarTheme();
        ((FrameworkElement)this.Content).RequestedTheme = theme.ToElementTheme();
    }

    private NowPlayingNotificationCorner _currentCorner = NowPlayingNotificationCorner.BottomLeft;

    public void Init(DisplayArea displayArea, NowPlayingNotificationCorner corner)
    {
        _currentCorner = corner;
        var targetRect = displayArea.WorkArea;
        AppWindow.MoveAndResize(targetRect);

        switch (corner)
        {
            case NowPlayingNotificationCorner.TopLeft:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Left;
                PopupContainer.VerticalAlignment = VerticalAlignment.Top;
                break;
            case NowPlayingNotificationCorner.TopCenter:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Center;
                PopupContainer.VerticalAlignment = VerticalAlignment.Top;
                break;
            case NowPlayingNotificationCorner.TopRight:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Right;
                PopupContainer.VerticalAlignment = VerticalAlignment.Top;
                break;
            case NowPlayingNotificationCorner.LeftCenter:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Left;
                PopupContainer.VerticalAlignment = VerticalAlignment.Center;
                break;
            case NowPlayingNotificationCorner.RightCenter:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Right;
                PopupContainer.VerticalAlignment = VerticalAlignment.Center;
                break;
            case NowPlayingNotificationCorner.BottomLeft:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Left;
                PopupContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case NowPlayingNotificationCorner.BottomCenter:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Center;
                PopupContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case NowPlayingNotificationCorner.BottomRight:
                PopupContainer.HorizontalAlignment = HorizontalAlignment.Right;
                PopupContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
        }

        this.SetIsAlwaysOnTop(true);
        this.Hide();
    }

    public async Task ShowAsync(SongInfo song, byte[]? albumArtBytes)
    {
        _hideTimer.Stop();

        TitleTextBlock.Text = song.Title;
        ArtistTextBlock.Text = song.Artist;

        var aumid = _gsmtcService.CurrentMediaSourceProviderInfo?.Provider;
        if (!string.IsNullOrEmpty(aumid))
        {
            AppExtensions.SetAumid(SourceLogoImage, aumid);
            AppExtensions.SetAumid(SourceNameTextBlock, aumid);
        }
        else
        {
            SourceLogoImage.Source = null;
            SourceNameTextBlock.Text = "";
        }

        if (albumArtBytes != null && albumArtBytes.Length > 0)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                using var stream = new MemoryStream(albumArtBytes);
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                AlbumArtImage.Source = bitmapImage;
                BackgroundArtImage.Source = bitmapImage;
            }
            catch
            {
                AlbumArtImage.Source = null;
                BackgroundArtImage.Source = null;
            }
        }
        else
        {
            AlbumArtImage.Source = null;
            BackgroundArtImage.Source = null;
        }

        User32.ShowWindow(WindowNative.GetWindowHandle(this), ShowWindowCommand.SW_SHOWNOACTIVATE);

        // Simple entrance animation
        if (!_isShowing)
        {
            _isShowing = true;
            PopupContainer.Opacity = 0;

            var isTop = _currentCorner == NowPlayingNotificationCorner.TopLeft || _currentCorner == NowPlayingNotificationCorner.TopCenter || _currentCorner == NowPlayingNotificationCorner.TopRight;
            var startY = isTop ? -30 : 30;

            PopupTranslateTransform.Y = startY;

            var sb = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeAnimation, PopupContainer);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            var slideAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideAnimation, PopupTranslateTransform);
            Storyboard.SetTargetProperty(slideAnimation, "Y");

            sb.Children.Add(fadeAnimation);
            sb.Children.Add(slideAnimation);
            sb.Begin();
        }
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        if (settingsService != null)
        {
            _hideTimer.Interval = TimeSpan.FromMilliseconds(settingsService.AppSettings.GeneralSettings.NowPlayingNotificationDuration);
        }

        _hideTimer.Start();
    }

    private void HideTimer_Tick(object? sender, object e)
    {
        _hideTimer.Stop();

        if (_isShowing)
        {
            var isTop = _currentCorner == NowPlayingNotificationCorner.TopLeft || _currentCorner == NowPlayingNotificationCorner.TopCenter || _currentCorner == NowPlayingNotificationCorner.TopRight;
            var endY = isTop ? -20 : 20;

            var sb = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeAnimation, PopupContainer);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            var slideAnimation = new DoubleAnimation
            {
                To = endY,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(slideAnimation, PopupTranslateTransform);
            Storyboard.SetTargetProperty(slideAnimation, "Y");

            sb.Children.Add(fadeAnimation);
            sb.Children.Add(slideAnimation);

            sb.Completed += (s, args) =>
            {
                this.Hide();
                _isShowing = false;
            };
            sb.Begin();
        }
    }
}
