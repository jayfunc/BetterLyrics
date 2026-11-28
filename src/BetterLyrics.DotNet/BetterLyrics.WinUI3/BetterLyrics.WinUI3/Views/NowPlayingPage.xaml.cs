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
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Renderer;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
using Win32Interop = Microsoft.UI.Win32Interop;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class NowPlayingPage : BasePage,
    IRecipient<CurrentSongInfoChangedMessage>,
    IRecipient<LyricsWindowLayoutProfileIdChangedMessage>,
    IRecipient<MappedSongSearchQueryChangedMessage>,
    IRecipient<LyricsWindowPaletteChangedMessage>,
    IRecipient<LyricsFontFamilyChangedMessage>,
    IRecipient<LyricsWindowLockConfigChangedMessage>,
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

    private readonly DispatcherTimer _lockedHoverPollTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };

    private IntPtr _windowHandle;

    private Rect _songTitleBounds;
    private Rect _songArtistBounds;
    private Rect _songAlbumBounds;

    private SongInfoAlignment _songTitleAlignment;
    private SongInfoAlignment _songArtistAlignment;
    private SongInfoAlignment _songAlbumAlignment;

    public NowPlayingPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();
        _lockedHoverPollTimer.Tick += LockedHoverPollTimer_Tick;
    }

    public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

    public NowPlayingBar PlaybackControlBar => NowPlayingBar;

    public void Receive(LayoutChangedMessage message)
    {
        OnLayoutChanged();
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
        ResetSongInfoHover();
    }

    public void Receive(LyricsFontFamilyChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus?.LyricsStyleSettings)
            _ = RenderSongInfoAsync();
    }

    public void Receive(LyricsWindowLockConfigChangedMessage message)
    {
        if (message.Sender == _lyricsWindowStatus)
            UpdateLockedHoverTracking();
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NowPlayingPage page)
        {
            if (e.Property == LyricsWindowStatusProperty)
            {
                page._lyricsWindowStatus = (LyricsWindowStatus?)e.NewValue;
                page.UpdateLockedHoverTracking();
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

            var titleFontSize = GetSongInfoFontSize(SongTitleContainer);
            var artistFontSize = GetSongInfoFontSize(SongArtistContainer);
            var albumFontSize = GetSongInfoFontSize(SongAlbumContainer);

            RenderTextBlock(TitleTextBlock, mappedTitle, titleFontSize);
            RenderTextBlock(ArtistsTextBlock, mappedArtist, artistFontSize);
            RenderTextBlock(AlbumTextBlock, mappedAlbum, albumFontSize);

            // 立即完成布局，让画布读到新文本与区域的实际尺寸
            UpdateLayout();

            UpdateSongInfoLayout();

            NowPlayingCanvas.SetSongInfo(mappedTitle, mappedArtist, mappedAlbum,
                titleFontSize, artistFontSize, albumFontSize);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RenderSongInfoAsync: {ex}");
        }
    }

    private void UpdateSongInfoLayout()
    {
        if (NowPlayingCanvas == null) return;

        _songTitleBounds = GetSongInfoBounds(SongTitleContainer);
        _songArtistBounds = GetSongInfoBounds(SongArtistContainer);
        _songAlbumBounds = GetSongInfoBounds(SongAlbumContainer);

        NowPlayingCanvas.SetSongInfoLayout(_songTitleBounds, _songArtistBounds, _songAlbumBounds);
        NowPlayingCanvas.SetSongInfoAlignment(SongInfoKind.Title, _songTitleAlignment);
        NowPlayingCanvas.SetSongInfoAlignment(SongInfoKind.Artist, _songArtistAlignment);
        NowPlayingCanvas.SetSongInfoAlignment(SongInfoKind.Album, _songAlbumAlignment);
    }

    // 字号按组件占据的网格单元高度计算。垂直对齐不是 Stretch 时，容器高度由占位文本撑开，
    // 若再用容器 ActualHeight 推导字号，每次重新布局都会按自身字号再缩放一次，窗口反复缩放后文本越来越小。
    private double GetSongInfoFontSize(Grid? container)
    {
        if (container == null) return 0;

        var height = !double.IsNaN(container.Height) && container.Height > 0
            ? container.Height
            : GetSongInfoCellHeight(container);

        if (!(height > 0)) height = container.ActualHeight;

        return Math.Max(0, height * 0.75);
    }

    private double GetSongInfoCellHeight(FrameworkElement element)
    {
        var rows = DynamicLayoutGrid?.RowDefinitions;
        if (rows == null || rows.Count == 0) return 0;

        var start = Math.Clamp(Grid.GetRow(element), 0, rows.Count - 1);
        var end = Math.Clamp(start + Math.Max(1, Grid.GetRowSpan(element)), start + 1, rows.Count);

        double height = 0;
        for (var i = start; i < end; i++) height += rows[i].ActualHeight;
        height += DynamicLayoutGrid!.RowSpacing * (end - start - 1);
        height -= element.Margin.Top + element.Margin.Bottom;

        return height;
    }

    private Rect GetSongInfoBounds(Grid? container)
    {
        if (container == null || RootGrid == null) return new Rect();
        if (container.Visibility != Visibility.Visible) return new Rect();
        if (container.ActualWidth <= 0 || container.ActualHeight <= 0) return new Rect();

        var bounds = container.TransformToVisual(RootGrid)
            .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
        var padding = container.Padding;

        return new Rect(
            bounds.X + padding.Left,
            bounds.Y + padding.Top,
            Math.Max(0, bounds.Width - padding.Left - padding.Right),
            Math.Max(0, bounds.Height - padding.Top - padding.Bottom));
    }

    private async Task RefreshSongInfoAsync()
    {
        try
        {
            NowPlayingCanvas.SetSongInfoOpacityTarget(0);
            await Task.Delay(Time.AnimationDuration);
            await RenderSongInfoAsync();
            NowPlayingCanvas.SetSongInfoOpacityTarget(1);
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

                // 歌曲信息由画布绘制：容器需要占满所在格子，视口才是正确的可滚动宽度
                if (double.IsNaN(placement.Width) &&
                    placement.ComponentType is ComponentType.SongTitle
                        or ComponentType.SongArtist
                        or ComponentType.SongAlbum)
                {
                    targetElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                }

                // 封面同样由画布绘制，空容器不再有内容撑开尺寸，未显式指定宽高时填满所在格子
                if (placement.ComponentType == ComponentType.AlbumArt)
                {
                    if (double.IsNaN(placement.Width))
                        targetElement.HorizontalAlignment = HorizontalAlignment.Stretch;

                    if (double.IsNaN(placement.Height))
                        targetElement.VerticalAlignment = VerticalAlignment.Stretch;
                }

                switch (placement.ComponentType)
                {
                    case ComponentType.AlbumArt:
                        // 封面由画布绘制，容器始终填满格子，对齐交给画布按真实图片边界摆放
                        NowPlayingCanvas.AlbumArtHorizontalAlignment = placement.HorizontalAlignment;
                        NowPlayingCanvas.AlbumArtVerticalAlignment = placement.VerticalAlignment;
                        break;
                    case ComponentType.SongTitle:
                        _songTitleAlignment = GetSongInfoAlignment(placement);
                        break;
                    case ComponentType.SongArtist:
                        _songArtistAlignment = GetSongInfoAlignment(placement);
                        break;
                    case ComponentType.SongAlbum:
                        _songAlbumAlignment = GetSongInfoAlignment(placement);
                        break;
                }
            }
        }
    }

    private static SongInfoAlignment GetSongInfoAlignment(ComponentPlacement placement)
    {
        // 设置了显式宽度时，对齐作用于组件盒子本身，盒内文本保持左对齐
        if (!double.IsNaN(placement.Width)) return SongInfoAlignment.Left;

        return placement.HorizontalAlignment switch
        {
            AppHorizontalAlignment.Center => SongInfoAlignment.Center,
            AppHorizontalAlignment.Right => SongInfoAlignment.Right,
            _ => SongInfoAlignment.Left
        };
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

        // 容器折叠后 ActualWidth 会保留旧值，立即清空画布里的视口，避免隐藏后仍绘制
        NowPlayingCanvas.AlbumArtRect = new Rect();
        _songTitleBounds = new Rect();
        _songArtistBounds = new Rect();
        _songAlbumBounds = new Rect();
        NowPlayingCanvas?.SetSongInfoLayout(_songTitleBounds, _songArtistBounds, _songAlbumBounds);
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

        // 布局配置里没有封面时容器保持折叠，ActualWidth 会保留上一次的值，
        // 不检查可见性会把旧矩形重新写回画布，导致封面无法隐藏
        if (AlbumArtContainer.Visibility != Visibility.Visible)
        {
            NowPlayingCanvas.AlbumArtRect = new Rect();
            return;
        }

        var transform = AlbumArtContainer.TransformToVisual(RootGrid);
        var localRect =
            new Rect(0, 0, AlbumArtContainer.ActualWidth, AlbumArtContainer.ActualHeight);
        NowPlayingCanvas.AlbumArtRect = transform.TransformBounds(localRect);
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

    private void ResetSongInfoHover()
    {
        NowPlayingCanvas?.SetSongInfoHover(SongInfoKind.Title, false);
        NowPlayingCanvas?.SetSongInfoHover(SongInfoKind.Artist, false);
        NowPlayingCanvas?.SetSongInfoHover(SongInfoKind.Album, false);
    }

    // 锁定窗口除播放栏外均为鼠标穿透（WS_EX_TRANSPARENT），拿不到 PointerEntered，
    // 故在锁定期间轮询光标位置来驱动歌曲信息的悬停滚动
    private bool IsLockedHoverTrackingRequired =>
        _lyricsWindowStatus is { IsLocked: true, IsWallpaper: false };

    private void UpdateLockedHoverTracking()
    {
        if (IsLockedHoverTrackingRequired)
            _lockedHoverPollTimer.Start();
        else
            StopLockedHoverTracking();
    }

    private void StopLockedHoverTracking()
    {
        if (!_lockedHoverPollTimer.IsEnabled) return;

        _lockedHoverPollTimer.Stop();
        ResetSongInfoHover();
    }

    private void LockedHoverPollTimer_Tick(object? sender, object e)
    {
        if (!IsLockedHoverTrackingRequired)
        {
            StopLockedHoverTracking();
            return;
        }

        if (!Vanara.PInvoke.User32.GetCursorPos(out var cursor)) return;

        UpdateSongInfoHoverFromCursor(cursor);
    }

    private void UpdateSongInfoHover(Point position)
    {
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Title, _songTitleBounds.Contains(position));
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Artist, _songArtistBounds.Contains(position));
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Album, _songAlbumBounds.Contains(position));
    }

    private void UpdateSongInfoHoverFromCursor(Vanara.PInvoke.POINT cursor)
    {
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Title, IsCursorOver(_songTitleBounds, cursor));
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Artist, IsCursorOver(_songArtistBounds, cursor));
        NowPlayingCanvas.SetSongInfoHover(SongInfoKind.Album, IsCursorOver(_songAlbumBounds, cursor));
    }

    private bool IsCursorOver(Rect bounds, Vanara.PInvoke.POINT cursor)
    {
        try
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;
            if (RootGrid.XamlRoot is not { IsHostVisible: true } xamlRoot) return false;

            var hwnd = GetWindowHandle(RootGrid);
            if (hwnd == IntPtr.Zero) return false;

            var topLeft = RootGrid.TransformToVisual(null)
                .TransformPoint(new Point(bounds.X, bounds.Y));
            var scale = xamlRoot.RasterizationScale;

            var point = new Vanara.PInvoke.POINT
            {
                X = (int)(topLeft.X * scale),
                Y = (int)(topLeft.Y * scale)
            };
            Vanara.PInvoke.User32.ClientToScreen(hwnd, ref point);

            return cursor.X >= point.X && cursor.X <= point.X + bounds.Width * scale &&
                   cursor.Y >= point.Y && cursor.Y <= point.Y + bounds.Height * scale;
        }
        catch
        {
            return false;
        }
    }

    private IntPtr GetWindowHandle(FrameworkElement element)
    {
        if (_windowHandle != IntPtr.Zero) return _windowHandle;
        if (element.XamlRoot == null) return IntPtr.Zero;

        _windowHandle = Win32Interop.GetWindowFromWindowId(element.XamlRoot.ContentIslandEnvironment.AppWindowId);
        return _windowHandle;
    }

    private void RootGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerWheelChanged(e);
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerMoved(e);
        UpdateSongInfoHover(e.GetCurrentPoint(RootGrid).Position);
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerReleased(e);
    }

    private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerExited(e);
        ResetSongInfoHover();
    }

    private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        NowPlayingCanvas.HandlePointerEntered(e);
        UpdateSongInfoHover(e.GetCurrentPoint(RootGrid).Position);
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
        UpdateLockedHoverTracking();

        if (_gsmtcService is not IBuiltInGsmtcService) return;

        // 内置播放器专用服务的消息走独立通道，不再接收默认通道（当前 GSMTC 会话）的同类消息
        var messenger = WeakReferenceMessenger.Default;
        messenger.Unregister<CurrentSongInfoChangedMessage>(this);
        messenger.Register<NowPlayingPage, CurrentSongInfoChangedMessage, string>(this, GsmtcMessageToken.BuiltIn,
            static (r, m) => r.Receive(m));
    }

    protected override void OnUnloaded()
    {
        StopLockedHoverTracking();
        WeakReferenceMessenger.Default.UnregisterAll(this, GsmtcMessageToken.BuiltIn);
        _layoutChangedDebouncer.Dispose();
    }

    private void DynamicLayoutGrid_Loaded(object sender, RoutedEventArgs e)
    {
        OnLayoutChanged();
    }

    private void NowPlayingCanvas_Loaded(object sender, RoutedEventArgs e)
    {
        NowPlayingCanvas.ParallaxContext = _parallaxEffect;
    }
}