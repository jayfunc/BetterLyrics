using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using BetterLyrics.Core.Effects;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.Avalonia.Controls;

namespace BetterLyrics.Avalonia.Views;

public partial class NowPlayingPage : UserControl,
    IRecipient<PropertyChangedMessage<SongInfo>>,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<Guid>>,
    IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>,
    IRecipient<PropertyChangedMessage<NowPlayingPalette>>,
    IRecipient<PropertyChangedMessage<float>>,
    IRecipient<LayoutChangedMessage>
{
    public static readonly StyledProperty<LyricsWindowStatus?> LyricsWindowStatusProperty =
        AvaloniaProperty.Register<NowPlayingPage, LyricsWindowStatus?>(nameof(LyricsWindowStatus));

    private readonly IGlobalToastProvider _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
    private readonly IFilePickerProvider _filePickerProvider = Ioc.Default.GetRequiredService<IFilePickerProvider>();
    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    private readonly Debouncer _layoutChangedDebouncer = new();
    private readonly ParallaxTiltEffect _parallaxEffect = new();
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly ISongSearchMapService _songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
    private readonly IAppUIThreadProvider _appUiThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    public NowPlayingPage()
    {
        InitializeComponent();
        DataContext = this;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public NowPlayingPageViewModel ViewModel => Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();

    public LyricsWindowStatus? LyricsWindowStatus
    {
        get => GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    // Listens for StyledProperty changes natively in Avalonia
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LyricsWindowStatusProperty)
        {
            OnLayoutChanged();
        }
    }

    public void Receive(LayoutChangedMessage message) => OnLayoutChanged();

    public void Receive(PropertyChangedMessage<bool> message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtAreaEffectSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.SongInfoAutoScroll))
                {
                    //UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
                }
                else if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.FadeOut))
                {
                    ToggleAlbumArtFadeOut();
                }
            }
        });
    }

    public void Receive(PropertyChangedMessage<float> message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtAreaEffectSettings)
            {
                if (message.PropertyName is nameof(AlbumArtAreaEffectSettings.FadeOutStartPointX) or
                    nameof(AlbumArtAreaEffectSettings.FadeOutStartPointY) or
                    nameof(AlbumArtAreaEffectSettings.FadeOutEndPointX) or
                    nameof(AlbumArtAreaEffectSettings.FadeOutEndPointY))
                {
                    UpdateAlbumArtFadeOutDirection();
                }
            }
        });
    }

    public void Receive(PropertyChangedMessage<Guid> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.LayoutProfileId))
            Dispatcher.UIThread.InvokeAsync(OnLayoutChanged);
    }

    public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
    {
        if (message.Sender is LyricsSearchControlViewModel && message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
            Dispatcher.UIThread.InvokeAsync(() => _ = RefreshSongInfoAsync());
    }

    public void Receive(PropertyChangedMessage<NowPlayingPalette> message)
    {
        if (message.Sender is LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.WindowPalette))
            Dispatcher.UIThread.InvokeAsync(() => _ = RenderSongInfoAsync());
    }

    public void Receive(PropertyChangedMessage<SongInfo> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentSongInfo))
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _ = RefreshSongInfoAsync();
                //UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
            });
        }
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
        {
            if (message.PropertyName is nameof(LyricsStyleSettings.LyricsCJKFontFamily) or nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                Dispatcher.UIThread.InvokeAsync(() => _ = RenderSongInfoAsync());
        }
    }

    private void RenderTextBlock(TextBlock? sender, string? text, double fontSize)
    {
        if (sender == null || !double.IsNormal(fontSize) || text == null || LyricsWindowStatus == null) return;

        var lyricsStyleSettings = LyricsWindowStatus.LyricsStyleSettings;

        sender.Inlines?.Clear();
        foreach (var ch in text)
        {
            var fontFamilyName = LanguageHelper.IsCJK(ch)
                ? lyricsStyleSettings.LyricsCJKFontFamily
                : lyricsStyleSettings.LyricsWesternFontFamily;

            sender.Inlines?.Add(new Run { Text = $"{ch}", FontFamily = new FontFamily(fontFamilyName) });
        }

        sender.FontSize = fontSize;
        sender.Foreground = new SolidColorBrush(ColorExtensions.FromAppColor(LyricsWindowStatus.WindowPalette.NonCurrentLineFillColor));
    }

    private async Task RenderSongInfoAsync()
    {
        if (LyricsWindowStatus == null) return;

        var (mappedTitle, mappedArtist, mappedAlbum) = await _songSearchMapService.GetMappingAsync(_gsmtcService.CurrentSongInfo);

        LyricsCard.Title = mappedTitle;
        LyricsCard.Artist = mappedArtist;

        var titleFontSize = SongTitleContainer.Bounds.Height * 0.75;
        var artistFontSize = SongArtistContainer.Bounds.Height * 0.75;
        var albumFontSize = SongAlbumContainer.Bounds.Height * 0.75;

        RenderTextBlock(TitleTextBlock, mappedTitle, titleFontSize);
        RenderTextBlock(ArtistsTextBlock, mappedArtist, artistFontSize);
        RenderTextBlock(AlbumTextBlock, mappedAlbum, albumFontSize);
    }

    private async Task RefreshSongInfoAsync()
    {
        SongTitleContainer.Opacity = 0;
        SongArtistContainer.Opacity = 0;
        SongAlbumContainer.Opacity = 0;
        await Task.Delay(Core.Constants.Time.AnimationDuration);
        await RenderSongInfoAsync();
        SongTitleContainer.Opacity = 1;
        SongArtistContainer.Opacity = 1;
        SongAlbumContainer.Opacity = 1;
    }

    private void ApplyLayoutProfile()
    {
        var profile = _settingsService.AppSettings.LayoutProfiles.FirstOrDefault(x => x.Id == LyricsWindowStatus?.LayoutProfileId);
        if (profile == null) return;

        DynamicLayoutGrid.Margin = new Thickness(profile.PaddingLeft, profile.PaddingTop, profile.PaddingRight, profile.PaddingBottom);

        DynamicLayoutGrid.RowDefinitions.Clear();
        DynamicLayoutGrid.ColumnDefinitions.Clear();

        foreach (var row in profile.RowDefinitions)
            DynamicLayoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLengthExtensions.ParseGridLength(row) });

        foreach (var col in profile.ColumnDefinitions)
            DynamicLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthExtensions.ParseGridLength(col) });

        foreach (var placement in profile.Placements)
        {
            Control? targetElement = placement.ComponentType switch
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
                targetElement.IsVisible = true;
                Grid.SetRow(targetElement, placement.Row);
                Grid.SetColumn(targetElement, placement.Column);
                Grid.SetRowSpan(targetElement, placement.RowSpan);
                Grid.SetColumnSpan(targetElement, placement.ColumnSpan);

                targetElement.Margin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight, placement.MarginBottom);
                targetElement.Width = placement.Width;
                targetElement.Height = placement.Height;

                targetElement.HorizontalAlignment = HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment);
                targetElement.VerticalAlignment = VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment);
            }
        }
    }

    private void ShowContainers()
    {
        var profile = _settingsService.AppSettings.LayoutProfiles.FirstOrDefault(x => x.Id == LyricsWindowStatus?.LayoutProfileId);
        if (profile == null) return;

        foreach (var placement in profile.Placements)
        {
            Control? targetElement = placement.ComponentType switch
            {
                ComponentType.Lyrics => LyricsContainer,
                ComponentType.LyricsCard => LyricsCardContainer,
                ComponentType.AlbumArt => AlbumArtContainer,
                ComponentType.SongTitle => SongTitleContainer,
                ComponentType.SongArtist => SongArtistContainer,
                ComponentType.SongAlbum => SongAlbumContainer,
                _ => null
            };

            targetElement?.IsVisible = true;
        }
    }

    private void HideContainers()
    {
        LyricsContainer.IsVisible = false;
        LyricsCardContainer.IsVisible = false;
        AlbumArtContainer.IsVisible = false;
        SongTitleContainer.IsVisible = false;
        SongArtistContainer.IsVisible = false;
        SongAlbumContainer.IsVisible = false;
    }

    private void UpdateLyricsLayout()
    {
        if (RootGrid == null || LyricsContainer == null || NowPlayingCanvas == null || LyricsWindowStatus == null) return;
        if (!LyricsContainer.IsLoaded || !RootGrid.IsLoaded) return;

        if (!LyricsContainer.IsVisible)
        {
            NowPlayingCanvas.LyricsOpacity = 0;
        }
        else
        {
            NowPlayingCanvas.LyricsOpacity = 1;

            // Avalonia uses TranslatePoint for visual translations
            var topLeft = LyricsContainer.TranslatePoint(new Point(0, 0), RootGrid);
            if (topLeft.HasValue)
            {
                NowPlayingCanvas.LyricsStartX = topLeft.Value.X;
                NowPlayingCanvas.LyricsStartY = topLeft.Value.Y;
                NowPlayingCanvas.LyricsWidth = LyricsContainer.Bounds.Width;
                NowPlayingCanvas.LyricsHeight = LyricsContainer.Bounds.Height;
            }
        }
    }

    private void UpdateAlbumArtLayout()
    {
        if (RootGrid == null || AlbumArtContainer == null) return;
        if (!AlbumArtContainer.IsLoaded || !RootGrid.IsLoaded) return;

        var topLeft = AlbumArtContainer.TranslatePoint(new Point(0, 0), RootGrid);
        if (topLeft.HasValue)
        {
            NowPlayingCanvas.AlbumArtRect = new Rect(topLeft.Value.X, topLeft.Value.Y, AlbumArtContainer.Bounds.Width, AlbumArtContainer.Bounds.Height);
        }

        ToggleAlbumArtFadeOut();
        UpdateAlbumArtFadeOutDirection();
    }

    private void OnLayoutChanged()
    {
        _ = _layoutChangedDebouncer.RunAsync(async () =>
        {
            HideContainers();
            ApplyLayoutProfile();

            await Task.Delay(100);

            UpdateLyricsLayout();
            UpdateAlbumArtLayout();
            await RenderSongInfoAsync();
            ShowContainers();
        });
    }

    private void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs e) => OnLayoutChanged();
    private void LyricsContainer_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateLyricsLayout();
    private void AlbumArtContainer_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateAlbumArtLayout();

    private void UpdateAutoScrollViewIsPlaying(object element, bool isPointerEntered)
    {
        // Cast to your custom element

        if (element is not AutoScrollView autoScrollView) return;

        if (LyricsWindowStatus?.AlbumArtAreaEffectSettings.SongInfoAutoScroll == true)
            autoScrollView.IsPlaying = true;
        else
            autoScrollView.IsPlaying = isPointerEntered;
    }

    private void TitleAutoScrollHoverEffectView_PointerEntered(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, true);
    private void TitleAutoScrollHoverEffectView_PointerExited(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);

    private void ArtistsAutoScrollHoverEffectView_PointerEntered(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, true);
    private void ArtistsAutoScrollHoverEffectView_PointerExited(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);

    private void AlbumAutoScrollHoverEffectView_PointerEntered(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, true);
    private void AlbumAutoScrollHoverEffectView_PointerExited(object? sender, PointerEventArgs e) => UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);

    private void RootGrid_PointerWheelChanged(object? sender, PointerWheelEventArgs e) => NowPlayingCanvas.HandlePointerWheelChanged(sender, e);
    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e) => NowPlayingCanvas.HandlePointerMoved(sender, e);
    private void RootGrid_PointerReleased(object? sender, PointerReleasedEventArgs e) => NowPlayingCanvas.HandlePointerReleased(sender, e);
    private void RootGrid_PointerExited(object? sender, PointerEventArgs e) => NowPlayingCanvas.HandlePointerExited(sender, e);

    private void RootGrid_PointerEntered(object? sender, PointerEventArgs e) => NowPlayingCanvas.HandlePointerEntered(sender, e);
    private void RootGrid_PointerPressed(object? sender, PointerPressedEventArgs e) => NowPlayingCanvas.HandlePointerPressed(sender, e);

    private async void SaveAlbumArtButton_Click(object? sender, RoutedEventArgs e)
    {
        var imageBytes = ViewModel.MediaSessionsService.AlbumArtBytes;
        if (imageBytes == null || imageBytes.Length == 0) return;

        IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
        {
            { "PNG", new List<string> { ".png" } },
            { "JPEG", new List<string> { ".jpg", ".jpeg" } }
        };

        var (_, filePath) = await _filePickerProvider.PickSaveFileAsync(fileTypeChoices, null, WindowType.NowPlayingWindow, LyricsWindowStatus);

        if (filePath != null)
        {
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
    }

    private void Page_Unloaded(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        DataContext = null;
        _layoutChangedDebouncer.Dispose();
    }

    private void DynamicLayoutGrid_Loaded(object? sender, RoutedEventArgs e) => OnLayoutChanged();
    private void AlbumArtParallaxTiltControl_Loaded(object? sender, RoutedEventArgs e) => AlbumArtParallaxTiltControl.ParallaxContext = _parallaxEffect;
    private void SongTitleParallaxTiltControl_Loaded(object? sender, RoutedEventArgs e) => SongTitleParallaxTiltControl.ParallaxContext = _parallaxEffect;
    private void SongArtistParallaxTiltControl_Loaded(object? sender, RoutedEventArgs e) => SongAristParallaxTiltControl.ParallaxContext = _parallaxEffect;
    private void SongAlbumParallaxTiltControl_Loaded(object? sender, RoutedEventArgs e) => SongAlbumParallaxTiltControl.ParallaxContext = _parallaxEffect;
    private void NowPlayingCanvas_Loaded(object? sender, RoutedEventArgs e) => NowPlayingCanvas.ParallaxContext = _parallaxEffect;

    private void UpdateAlbumArtFadeOutDirection()
    {
        var settings = LyricsWindowStatus?.AlbumArtAreaEffectSettings;

        var brush = (LinearGradientBrush?)AlbumArtBorder.OpacityMask;

        if (settings == null || brush == null) return;

        brush.StartPoint = new RelativePoint(settings.FadeOutStartPointX, settings.FadeOutStartPointY, RelativeUnit.Relative);
        brush.EndPoint = new RelativePoint(settings.FadeOutEndPointX, settings.FadeOutEndPointY, RelativeUnit.Relative);
    }

    private void ToggleAlbumArtFadeOut()
    {
        var endStop = ((LinearGradientBrush?)AlbumArtBorder.OpacityMask)?.GradientStops.LastOrDefault();
        if (endStop == null) return;

        endStop.Color = LyricsWindowStatus?.AlbumArtAreaEffectSettings.FadeOut == true
            ? Colors.Transparent
            : Colors.White;
    }
}