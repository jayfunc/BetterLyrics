// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Effects;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.LayoutChanged;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using ColorExtensions = BetterLyrics.WinUI3.Extensions.ColorExtensions;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class NowPlayingPage : BasePage,
    IRecipient<CurrentSongInfoChangedMessage>,
    IRecipient<AlbumArtSongInfoAutoScrollChangedMessage>,
    IRecipient<AlbumArtFadeOutChangedMessage>,
    IRecipient<AlbumArtFadeOutDirectionChangedMessage>,
    IRecipient<LyricsWindowLayoutProfileIdChangedMessage>,
    IRecipient<MappedSongSearchQueryChangedMessage>,
    IRecipient<LyricsWindowPaletteChangedMessage>,
    IRecipient<LyricsFontFamilyChangedMessage>,
    IRecipient<LayoutChangedMessage>
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingPage),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public static readonly DependencyProperty IsBuiltInOnlyProperty =
        DependencyProperty.Register(nameof(IsBuiltInOnly), typeof(bool), typeof(NowPlayingPage),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    /// <summary>
    /// 为 true 时仅跟踪并显示内置播放器，否则跟踪当前 GSMTC 会话。
    /// </summary>
    public bool IsBuiltInOnly
    {
        get => (bool)GetValue(IsBuiltInOnlyProperty);
        set => SetValue(IsBuiltInOnlyProperty, value);
    }

    private LyricsWindowStatus? _lyricsWindowStatus = null;

    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IFilePickerProvider _filePickerProvider = Ioc.Default.GetRequiredService<IFilePickerProvider>();

    private IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly Debouncer _layoutChangedDebouncer = new();

    private readonly ParallaxTiltEffect _parallaxEffect = new();

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly ISongSearchMapService _songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();

    public NowPlayingPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();
    }

    public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

    public NowPlayingBar PlaybackControlBar => NowPlayingBar;

    public void Receive(LayoutChangedMessage message)
    {
        OnLayoutChanged();
    }

    public void Receive(AlbumArtSongInfoAutoScrollChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.AlbumArtAreaEffectSettings)
        {
            UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
            UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
            UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
        }
    }

    public void Receive(AlbumArtFadeOutChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.AlbumArtAreaEffectSettings)
        {
            ToggleAlbumArtFadeOut();
        }
    }

    public void Receive(AlbumArtFadeOutDirectionChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.AlbumArtAreaEffectSettings)
        {
            UpdateAlbumArtFadeOutDirection();
        }
    }

    public void Receive(LyricsWindowLayoutProfileIdChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus)
            OnLayoutChanged();
    }

    public void Receive(MappedSongSearchQueryChangedMessage message)
    {
        _ = RefreshSongInfoAsync();
    }

    public void Receive(LyricsWindowPaletteChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus)
            _ = RenderSongInfoAsync();
    }

    public void Receive(CurrentSongInfoChangedMessage message)
    {
        _ = RefreshSongInfoAsync();
        UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
        UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
        UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
    }

    public void Receive(LyricsFontFamilyChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
            _ = RenderSongInfoAsync();
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NowPlayingPage page)
        {
            if (e.Property == LyricsWindowStatusProperty)
            {
                page._lyricsWindowStatus = (LyricsWindowStatus?)e.NewValue;
                page.OnLayoutChanged();
            }
            else if (e.Property == IsBuiltInOnlyProperty)
            {
                page.ApplyGsmtcSource((bool)e.NewValue);
            }
        }
    }

    private void RenderTextBlock(TextBlock? sender, string? text, double fontSize)
    {
        if (sender == null || !double.IsNormal(fontSize) || text == null || _lyricsWindowStatus == null) return;

        var lyricsStyleSettings = _lyricsWindowStatus.LyricsStyleSettings;

        sender.Inlines.Clear();
        foreach (var ch in text)
        {
            var fontFamilyName = LanguageHelper.IsCJK(ch)
                ? lyricsStyleSettings.LyricsCJKFontFamily
                : lyricsStyleSettings.LyricsWesternFontFamily;
            sender.Inlines.Add(new Run { Text = $"{ch}", FontFamily = new FontFamily(fontFamilyName) });
        }

        sender.FontSize = fontSize;
        sender.Foreground =
            new SolidColorBrush(
                ColorExtensions.FromAppColor(_lyricsWindowStatus.WindowPalette.NonCurrentLineFillColor));
    }

    private async Task RenderSongInfoAsync()
    {
        try
        {
            if (_lyricsWindowStatus == null) return;

            var (mappedTitle, mappedArtist, mappedAlbum) =
                await _songSearchMapService.GetMappingAsync(_gsmtcService.CurrentSongInfo);

            LyricsCard.Title = mappedTitle;
            LyricsCard.Artist = mappedArtist;

            var titleFontSize = SongTitleContainer.ActualHeight * 0.75;
            var artistFontSize = SongArtistContainer.ActualHeight * 0.75;
            var albumFontSize = SongAlbumContainer.ActualHeight * 0.75;

            RenderTextBlock(TitleTextBlock, mappedTitle, titleFontSize);
            RenderTextBlock(ArtistsTextBlock, mappedArtist, artistFontSize);
            RenderTextBlock(AlbumTextBlock, mappedAlbum, albumFontSize);

            if (double.IsNormal(titleFontSize))
                TitleAutoScrollHoverEffectView.ScrollingPixelsPreSecond = (int)titleFontSize;
            if (double.IsNormal(artistFontSize))
                ArtistsAutoScrollHoverEffectView.ScrollingPixelsPreSecond = (int)artistFontSize;
            if (double.IsNormal(albumFontSize))
                AlbumAutoScrollHoverEffectView.ScrollingPixelsPreSecond = (int)albumFontSize;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RenderSongInfoAsync: {ex}");
        }
    }

    private async Task RefreshSongInfoAsync()
    {
        try
        {
            SongTitleContainer.Opacity = 0;
            SongArtistContainer.Opacity = 0;
            SongAlbumContainer.Opacity = 0;
            await Task.Delay(Time.AnimationDuration);
            await RenderSongInfoAsync();
            SongTitleContainer.Opacity = 1;
            SongArtistContainer.Opacity = 1;
            SongAlbumContainer.Opacity = 1;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshSongInfoAsync: {ex}");
        }
    }

    private void ApplyLayoutProfile()
    {
        var profile =
            _settingsService.AppSettings.LayoutProfiles.FirstOrDefault(x =>
                x.Id == _lyricsWindowStatus?.LayoutProfileId);
        if (profile == null) return;

        DynamicLayoutGrid.Padding = new Thickness(
            profile.PaddingLeft,
            profile.PaddingTop,
            profile.PaddingRight,
            profile.PaddingBottom);

        DynamicLayoutGrid.ColumnSpacing = profile.ColumnSpacing;
        DynamicLayoutGrid.RowSpacing = profile.RowSpacing;

        DynamicLayoutGrid.RowDefinitions.Clear();
        DynamicLayoutGrid.ColumnDefinitions.Clear();

        foreach (var row in profile.RowDefinitions)
            DynamicLayoutGrid.RowDefinitions.Add(new RowDefinition
            { Height = GridLengthExtensions.ParseGridLength(row) });

        foreach (var col in profile.ColumnDefinitions)
            DynamicLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition
            { Width = GridLengthExtensions.ParseGridLength(col) });

        foreach (var placement in profile.Placements)
        {
            FrameworkElement? targetElement = placement.ComponentType switch
            {
                ComponentType.Lyrics => LyricsContainer,
                ComponentType.LyricsCard => LyricsCardContainer,
                ComponentType.AlbumArt => AlbumArtContainer,
                ComponentType.SongTitle => SongTitleContainer,
                ComponentType.SongArtist => SongArtistContainer,
                ComponentType.SongAlbum => SongAlbumContainer,
                ComponentType.PlaybackControl => PlaybackControlContainer,
                _ => null
            };

            if (targetElement != null)
            {
                targetElement.Visibility = Visibility.Visible;

                Grid.SetRow(targetElement, placement.Row);
                Grid.SetColumn(targetElement, placement.Column);
                Grid.SetRowSpan(targetElement, placement.RowSpan);
                Grid.SetColumnSpan(targetElement, placement.ColumnSpan);

                targetElement.Margin = new Thickness(
                    placement.MarginLeft,
                    placement.MarginTop,
                    placement.MarginRight,
                    placement.MarginBottom);

                var padding = new Thickness(
                    placement.PaddingLeft,
                    placement.PaddingTop,
                    placement.PaddingRight,
                    placement.PaddingBottom);

                if (placement.ComponentType == ComponentType.PlaybackControl)
                {
                    NowPlayingBar.Padding = padding;
                    if (targetElement is Grid grid) grid.Padding = new Thickness(0);
                }
                else
                {
                    if (targetElement is Grid grid) grid.Padding = padding;
                    else if (targetElement is Border border) border.Padding = padding;
                    else if (targetElement is Control control) control.Padding = padding;
                }

                targetElement.Width = placement.Width;
                targetElement.Height = placement.Height;

                targetElement.HorizontalAlignment =
                    HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment);
                targetElement.VerticalAlignment =
                    VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment);
            }
        }
    }

    private void ShowContainers()
    {
        var profile =
            _settingsService.AppSettings.LayoutProfiles.FirstOrDefault(x =>
                x.Id == _lyricsWindowStatus?.LayoutProfileId);
        if (profile == null) return;

        foreach (var placement in profile.Placements)
        {
            FrameworkElement? targetElement = placement.ComponentType switch
            {
                ComponentType.Lyrics => LyricsContainer,
                ComponentType.LyricsCard => LyricsCardContainer,
                ComponentType.AlbumArt => AlbumArtContainer,
                ComponentType.SongTitle => SongTitleContainer,
                ComponentType.SongArtist => SongArtistContainer,
                ComponentType.SongAlbum => SongAlbumContainer,
                ComponentType.PlaybackControl => PlaybackControlContainer,
                _ => null
            };

            if (targetElement != null) targetElement.Visibility = Visibility.Visible;
        }
    }

    private void HideContainers()
    {
        LyricsContainer.Visibility = Visibility.Collapsed;
        LyricsCardContainer.Visibility = Visibility.Collapsed;
        AlbumArtContainer.Visibility = Visibility.Collapsed;
        SongTitleContainer.Visibility = Visibility.Collapsed;
        SongArtistContainer.Visibility = Visibility.Collapsed;
        SongAlbumContainer.Visibility = Visibility.Collapsed;
        PlaybackControlContainer.Visibility = Visibility.Collapsed;
    }

    private void UpdateLyricsLayout()
    {
        if (RootGrid == null || LyricsContainer == null || NowPlayingCanvas == null) return;
        if (_lyricsWindowStatus == null) return;

        if (!LyricsContainer.IsLoaded || !RootGrid.IsLoaded) return;

        if (LyricsContainer.Visibility == Visibility.Collapsed)
        {
            NowPlayingCanvas.LyricsOpacity = 0;
        }
        else
        {
            NowPlayingCanvas.LyricsOpacity = 1;

            var transform = LyricsContainer.TransformToVisual(RootGrid);
            var localRect =
                new Rect(0, 0, NowPlayingCanvas.ActualWidth, NowPlayingCanvas.ActualHeight);
            var relativeRect = transform.TransformBounds(localRect);

            NowPlayingCanvas.LyricsStartX = relativeRect.X;
            NowPlayingCanvas.LyricsStartY = relativeRect.Y;
            NowPlayingCanvas.LyricsWidth = LyricsContainer.ActualWidth;
            NowPlayingCanvas.LyricsHeight = LyricsContainer.ActualHeight;
        }
    }

    private void UpdateAlbumArtLayout()
    {
        if (RootGrid == null || AlbumArtContainer == null) return;
        if (!AlbumArtContainer.IsLoaded || !RootGrid.IsLoaded) return;

        var transform = AlbumArtContainer.TransformToVisual(RootGrid);
        var localRect =
            new Rect(0, 0, AlbumArtContainer.ActualWidth, AlbumArtContainer.ActualHeight);
        NowPlayingCanvas.AlbumArtRect = transform.TransformBounds(localRect);

        ToggleAlbumArtFadeOut();
        UpdateAlbumArtFadeOutDirection();
    }

    private void UpdateNowPlayingBarLayout()
    {
        if (RootGrid == null || PlaybackControlContainer == null) return;
        if (!PlaybackControlContainer.IsLoaded || !RootGrid.IsLoaded) return;

        if (PlaybackControlContainer.Visibility == Visibility.Collapsed)
        {
            NowPlayingCanvas.NowPlayingBarRect = new Rect(0, 0, 0, 0);
            return;
        }

        var transform = PlaybackControlContainer.TransformToVisual(RootGrid);
        var localRect =
            new Rect(0, 0, PlaybackControlContainer.ActualWidth, PlaybackControlContainer.ActualHeight);
        NowPlayingCanvas.NowPlayingBarRect = transform.TransformBounds(localRect);
    }

    private void OnLayoutChanged()
    {
        _ = _layoutChangedDebouncer.RunAsync(async () =>
        {
            HideContainers();

            ApplyLayoutProfile();

            // Ensure the layout is updated before calculating positions
            await Task.Delay(100);

            UpdateLyricsLayout();
            UpdateAlbumArtLayout();
            UpdateNowPlayingBarLayout();

            await RenderSongInfoAsync();

            ShowContainers();
        });
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        OnLayoutChanged();
    }

    private void LyricsContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateLyricsLayout();
    }

    private void AlbumArtContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateAlbumArtLayout();
    }

    private void PlaybackControlContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateNowPlayingBarLayout();
    }

    private void UpdateAutoScrollViewIsPlaying(AutoScrollView element, bool isPointerEntered)
    {
        if (_lyricsWindowStatus?.AlbumArtAreaEffectSettings.SongInfoAutoScroll == true)
            element.IsPlaying = true;
        else
            element.IsPlaying = isPointerEntered;
    }

    private void TitleAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
    }

    private void TitleAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, true);
    }

    private void TitleAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
    }

    private void ArtistsAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
    }

    private void ArtistsAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, true);
    }

    private void ArtistsAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
    }

    private void AlbumAutoScrollHoverEffectView_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
    }

    private void AlbumAutoScrollHoverEffectView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, true);
    }

    private void AlbumAutoScrollHoverEffectView_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
    }

    private void RootGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerWheelChanged(e);
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerMoved(e);
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerReleased(e);
    }

    private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerExited(e);
    }

    private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerEntered(e);
    }

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerPressed(e);
    }

    private async void SaveAlbumArtButton_Click(object sender, RoutedEventArgs e)
    {
        var imageBytes = ViewModel.MediaSessionsService.AlbumArtBytes;

        if (imageBytes == null || imageBytes.Length == 0) return;

        IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
        {
            { "PNG", new List<string> { ".png" } },
            { "JPEG", new List<string> { ".jpg", ".jpeg" } }
        };

        var (_, filePath) =
            await _filePickerProvider.PickSaveFileAsync(fileTypeChoices, null, WindowType.NowPlayingWindow,
                _lyricsWindowStatus);

        if (filePath != null)
            try
            {
                await File.WriteAllBytesAsync(filePath, imageBytes);
                _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
            }
    }

    protected override void BindGeneratedControls()
    {
        Bindings?.Update();
    }

    protected override void UnbindGeneratedControls()
    {
        Bindings?.StopTracking();
    }

    private void ApplyGsmtcSource(bool isBuiltInOnly)
    {
        _gsmtcService = isBuiltInOnly
            ? Ioc.Default.GetRequiredService<IBuiltInGsmtcService>()
            : Ioc.Default.GetRequiredService<IGsmtcService>();

        ViewModel.MediaSessionsService = _gsmtcService;
        NowPlayingCanvas.GsmtcSource = _gsmtcService;
        LyricsCard.GsmtcSource = _gsmtcService;
        NowPlayingBar.IsBuiltInOnly = isBuiltInOnly;
    }

    protected override void OnLoaded()
    {
        if (_gsmtcService is not IBuiltInGsmtcService) return;

        // 内置播放器专用服务的消息走独立通道，不再接收默认通道（当前 GSMTC 会话）的同类消息
        var messenger = WeakReferenceMessenger.Default;
        messenger.Unregister<CurrentSongInfoChangedMessage>(this);
        messenger.Register<NowPlayingPage, CurrentSongInfoChangedMessage, string>(this, GsmtcMessageToken.BuiltIn,
            static (r, m) => r.Receive(m));
    }

    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this, GsmtcMessageToken.BuiltIn);
        _layoutChangedDebouncer.Dispose();
    }

    private void DynamicLayoutGrid_Loaded(object sender, RoutedEventArgs e)
    {
        OnLayoutChanged();
    }

    private void AlbumArtParallaxTiltControl_Loaded(object sender, RoutedEventArgs e)
    {
        AlbumArtParallaxTiltControl.ParallaxContext = _parallaxEffect;
    }

    private void SongTitleParallaxTiltControl_Loaded(object sender, RoutedEventArgs e)
    {
        SongTitleParallaxTiltControl.ParallaxContext = _parallaxEffect;
    }

    private void SongArtistParallaxTiltControl_Loaded(object sender, RoutedEventArgs e)
    {
        SongAristParallaxTiltControl.ParallaxContext = _parallaxEffect;
    }

    private void SongAlbumParallaxTiltControl_Loaded(object sender, RoutedEventArgs e)
    {
        SongAlbumParallaxTiltControl.ParallaxContext = _parallaxEffect;
    }

    private void NowPlayingCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        NowPlayingCanvas.ParallaxContext = _parallaxEffect;
    }

    private void UpdateAlbumArtFadeOutDirection()
    {
        var settings = _lyricsWindowStatus?.AlbumArtAreaEffectSettings;
        if (settings == null) return;

        var albumArtParallaxTiltControlWidth = AlbumArtParallaxTiltControl.ActualWidth;
        var albumArtParallaxTiltControlHeight = AlbumArtParallaxTiltControl.ActualHeight;

        var startPointX = ((albumArtParallaxTiltControlWidth - 32 * 2) * settings.FadeOutStartPointX + 32) / albumArtParallaxTiltControlWidth;
        var startPointY = ((albumArtParallaxTiltControlHeight - 32 * 2) * settings.FadeOutStartPointY + 32) / albumArtParallaxTiltControlHeight;
        var endPointX = ((albumArtParallaxTiltControlWidth - 32 * 2) * settings.FadeOutEndPointX + 32) / albumArtParallaxTiltControlWidth;
        var endPointY = ((albumArtParallaxTiltControlHeight - 32 * 2) * settings.FadeOutEndPointY + 32) / albumArtParallaxTiltControlHeight;

        AlbumArtGradientBrush.StartPoint = new Point(startPointX, startPointY);
        AlbumArtGradientBrush.EndPoint = new Point(endPointX, endPointY);
    }

    private void ToggleAlbumArtFadeOut()
    {
        AlbumArtGradientBrushEnd.Color = ColorExtensions.FromAppColor(
            _lyricsWindowStatus?.AlbumArtAreaEffectSettings.FadeOut == true
                ? Colors.Transparent
                : Colors.White);
    }
}