// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ColorExtensions = BetterLyrics.WinUI3.Extensions.ColorExtensions;
using BetterLyrics.Core.Effects;
using BetterLyrics.Core.ViewModels;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class NowPlayingPage : Page,
    IRecipient<PropertyChangedMessage<SongInfo>>,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<Guid>>,
    IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>,
    IRecipient<PropertyChangedMessage<NowPlayingPalette>>,
    IRecipient<PropertyChangedMessage<float>>,
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

    private LyricsWindowStatus? _lyricsWindowStatus = null;

    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly Debouncer _layoutChangedDebouncer = new();

    private readonly ParallaxTiltEffect _parallaxEffect = new();

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private readonly ISongSearchMapService _songSearchMapService =
        Ioc.Default.GetRequiredService<ISongSearchMapService>();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public NowPlayingPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

    public void Receive(LayoutChangedMessage message)
    {
        OnLayoutChanged();
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == _lyricsWindowStatus?.AlbumArtAreaEffectSettings)
        {
            if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.SongInfoAutoScroll))
            {
                UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
            }
            else if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOut))
            {
                ToggleAlbumArtFadeOut();
            }
        }
    }

    public void Receive(PropertyChangedMessage<float> message)
    {
        if (message.Sender == _lyricsWindowStatus?.AlbumArtAreaEffectSettings)
        {
            if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOutStartPointX))
                UpdateAlbumArtFadeOutDirection();
            else if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOutStartPointY))
                UpdateAlbumArtFadeOutDirection();
            else if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOutEndPointX))
                UpdateAlbumArtFadeOutDirection();
            else if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOutEndPointY))
                UpdateAlbumArtFadeOutDirection();
        }
    }

    public void Receive(PropertyChangedMessage<Guid> message)
    {
        if (message.Sender == _lyricsWindowStatus)
            if (message.PropertyName == nameof(_lyricsWindowStatus.LayoutProfileId))
                OnLayoutChanged();
    }

    public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
    {
        if (message.Sender is LyricsSearchControlViewModel)
            if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                _ = RefreshSongInfoAsync();
    }

    public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
    {
        if (message.Sender == _lyricsWindowStatus)
            if (message.PropertyName == nameof(_lyricsWindowStatus.WindowPalette))
                _ = RenderSongInfoAsync();
    }

    public void Receive(PropertyChangedMessage<SongInfo> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentSongInfo))
        {
            _ = RefreshSongInfoAsync();
            UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
            UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
            UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
        }
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                _ = RenderSongInfoAsync();
            else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                _ = RenderSongInfoAsync();
        }
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
    }

    private async Task RefreshSongInfoAsync()
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
                Debug.WriteLine($"SaveAlbumArtButton_Click: {ex}");
            }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        DataContext = null;

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

        AlbumArtGradientBrush.StartPoint =
            new Point(settings.FadeOutStartPointX, settings.FadeOutStartPointY);
        AlbumArtGradientBrush.EndPoint =
            new Point(settings.FadeOutEndPointX, settings.FadeOutEndPointY);
    }

    private void ToggleAlbumArtFadeOut()
    {
        AlbumArtGradientBrushEnd.Color = ColorExtensions.FromAppColor(
            _lyricsWindowStatus?.AlbumArtAreaEffectSettings.FadeOut == true
                ? Colors.Transparent
                : Colors.White);
    }
}