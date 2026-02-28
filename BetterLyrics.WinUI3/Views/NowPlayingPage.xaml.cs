// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.Lyrics;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using DevWinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingPage : Page,
        IRecipient<PropertyChangedMessage<SongInfo>>,
        IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>,
        IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>
    {
        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();
        private readonly ISongSearchMapService _songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();

        private DispatcherQueueTimer? _layoutChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();
        private DispatcherQueueTimer? _scrollChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();

        public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

        public LyricsWindowStatus? LyricsWindowStatus
        {
            get { return (LyricsWindowStatus?)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingPage), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public AlbumArtThemeColors AlbumArtThemeColors
        {
            get { return (AlbumArtThemeColors)GetValue(AlbumArtThemeColorsProperty); }
            set { SetValue(AlbumArtThemeColorsProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtThemeColorsProperty =
            DependencyProperty.Register(nameof(AlbumArtThemeColors), typeof(AlbumArtThemeColors), typeof(NowPlayingPage), new PropertyMetadata(new AlbumArtThemeColors(), OnDependencyPropertyChanged));

        public NowPlayingPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<NowPlayingPageViewModel>();

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NowPlayingPage page)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    page.OnLayoutChanged();
                    _ = page.RenderSongInfoAsync();
                }
                else if (e.Property == AlbumArtThemeColorsProperty)
                {
                    _ = page.RenderSongInfoAsync();
                }
            }
        }

        // ==== SongInfo

        private void RenderTextBlock(TextBlock? sender, string? text, double fontSize)
        {
            if (sender == null || text == null || fontSize == 0 || LyricsWindowStatus == null) return;

            var lyricsStyleSettings = LyricsWindowStatus.LyricsStyleSettings;

            sender.Inlines.Clear();
            foreach (var ch in text)
            {
                var fontFamilyName = LanguageHelper.IsCJK(ch) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;
                sender.Inlines.Add(new Run { Text = $"{ch}", FontFamily = new FontFamily(fontFamilyName) });
            }
            sender.FontSize = (int)fontSize;
            sender.Foreground = new SolidColorBrush(AlbumArtThemeColors.BgFontColor);
        }

        private async Task RenderSongInfoAsync()
        {
            if (LyricsWindowStatus == null) return;

            var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);

            var albumArtLayoutSettings = LyricsWindowStatus.AlbumArtLayoutSettings;

            var titleFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.SongTitleSize : albumArtLayoutSettings.SongInfoFontSize;
            var artistsFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.ArtistNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;
            var albumFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.AlbumNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;

            (string mappedTitle, string mappedArtist, string mappedAlbum) = await _songSearchMapService.GetMappingAsync(_gsmtcService.CurrentSongInfo);

            RenderTextBlock(TitleTextBlock, mappedTitle, titleFontSize);
            RenderTextBlock(ArtistsTextBlock, mappedArtist, artistsFontSize);
            RenderTextBlock(AlbumTextBlock, mappedAlbum, albumFontSize);
        }

        private void UpdateSongInfoOpacity()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    SongInfoStackPanel.Opacity = 1;
                    break;
                case LyricsDisplayType.LyricsOnly:
                    SongInfoStackPanel.Opacity = 0;
                    break;
                case LyricsDisplayType.SplitView:
                    SongInfoStackPanel.Opacity = 1;
                    break;
                default:
                    break;
            }
        }

        private async Task RefreshSongInfoAsync()
        {
            SongInfoStackPanel.Opacity = 0;
            await Task.Delay(Constants.Time.AnimationDuration);
            await RenderSongInfoAsync();
            SongInfoStackPanel.Opacity = 1;
            UpdateSongInfoOpacity();
        }

        // ==== AlbumArt
        private void UpdateAlbumArtOpacity()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    AlbumArtGrid.Opacity = 1;
                    break;
                case LyricsDisplayType.LyricsOnly:
                    AlbumArtGrid.Opacity = 0;
                    break;
                case LyricsDisplayType.SplitView:
                    AlbumArtGrid.Opacity = 1;
                    break;
                default:
                    break;
            }
        }

        // ====

        private void UpdateTrackSummaryGridSpan()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    Grid.SetRowSpan(TrackSummaryGrid, 3);
                    Grid.SetColumnSpan(TrackSummaryGrid, 3);
                    break;
                case LyricsDisplayType.LyricsOnly:
                    break;
                case LyricsDisplayType.SplitView:
                    switch (LyricsWindowStatus.LyricsLayoutOrientation)
                    {
                        case LyricsLayoutOrientation.Horizontal:
                            Grid.SetRowSpan(TrackSummaryGrid, 3);
                            Grid.SetColumnSpan(TrackSummaryGrid, 1);
                            break;
                        case LyricsLayoutOrientation.Vertical:
                            Grid.SetRowSpan(TrackSummaryGrid, 1);
                            Grid.SetColumnSpan(TrackSummaryGrid, 3);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        // ====

        private void UpdateSongInfoStackPanelSpan()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    Grid.SetRow(SongInfoStackPanel, 3);
                    Grid.SetRowSpan(SongInfoStackPanel, 1);
                    Grid.SetColumn(SongInfoStackPanel, 0);
                    Grid.SetColumnSpan(SongInfoStackPanel, 2);
                    break;
                case LyricsLayoutOrientation.Vertical:
                    Grid.SetRow(SongInfoStackPanel, 1);
                    Grid.SetRowSpan(SongInfoStackPanel, 3);
                    Grid.SetColumn(SongInfoStackPanel, 2);
                    Grid.SetColumnSpan(SongInfoStackPanel, 1);
                    break;
                default:
                    break;
            }
        }

        private void UpdateLyricsPlaceholderSpan()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    break;
                case LyricsDisplayType.LyricsOnly:
                    Grid.SetRow(LyricsPlaceholder, 0);
                    Grid.SetRowSpan(LyricsPlaceholder, 3);
                    Grid.SetColumn(LyricsPlaceholder, 1);
                    Grid.SetColumnSpan(LyricsPlaceholder, 3);
                    break;
                case LyricsDisplayType.SplitView:
                    switch (LyricsWindowStatus.LyricsLayoutOrientation)
                    {
                        case LyricsLayoutOrientation.Horizontal:
                            Grid.SetRow(LyricsPlaceholder, 0);
                            Grid.SetRowSpan(LyricsPlaceholder, 3);
                            Grid.SetColumn(LyricsPlaceholder, 3);
                            Grid.SetColumnSpan(LyricsPlaceholder, 1);
                            break;
                        case LyricsLayoutOrientation.Vertical:
                            Grid.SetRow(LyricsPlaceholder, 0);
                            Grid.SetRowSpan(LyricsPlaceholder, 3);
                            Grid.SetColumn(LyricsPlaceholder, 1);
                            Grid.SetColumnSpan(LyricsPlaceholder, 3);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        // ====

        private void UpdateAlbumArtGridSpan()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    Grid.SetRow(AlbumArtGrid, 1);
                    Grid.SetRowSpan(AlbumArtGrid, 1);
                    Grid.SetColumn(AlbumArtGrid, 0);
                    Grid.SetColumnSpan(AlbumArtGrid, 2);
                    break;
                case LyricsLayoutOrientation.Vertical:
                    Grid.SetRow(AlbumArtGrid, 1);
                    Grid.SetRowSpan(AlbumArtGrid, 3);
                    Grid.SetColumn(AlbumArtGrid, 0);
                    Grid.SetColumnSpan(AlbumArtGrid, 1);
                    break;
                default:
                    break;
            }
        }

        // ==== Lyrics

        private void UpdateLyricsOpacity()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    LyricsCanvas.LyricsOpacity = 0;
                    break;
                case LyricsDisplayType.LyricsOnly:
                case LyricsDisplayType.SplitView:
                    LyricsCanvas.LyricsOpacity = 1;
                    break;
                default:
                    break;
            }
        }

        private void UpdateLyricsLayout()
        {
            if (LyricsWindowStatus == null) return;

            switch (LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    break;
                case LyricsDisplayType.LyricsOnly:
                    LyricsCanvas.LyricsStartX = LeftGapDef.ActualWidth;
                    LyricsCanvas.LyricsStartY = 0;
                    LyricsCanvas.LyricsWidth = TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth + LyricsColDef.ActualWidth;
                    LyricsCanvas.LyricsHeight = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight + LyricsRowDef.ActualHeight;
                    break;
                case LyricsDisplayType.SplitView:
                    switch (LyricsWindowStatus.LyricsLayoutOrientation)
                    {
                        case LyricsLayoutOrientation.Horizontal:
                            LyricsCanvas.LyricsStartX = LeftGapDef.ActualWidth + TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth;
                            LyricsCanvas.LyricsStartY = 0;
                            LyricsCanvas.LyricsWidth = LyricsColDef.ActualWidth;
                            LyricsCanvas.LyricsHeight = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight + LyricsRowDef.ActualHeight;
                            break;
                        case LyricsLayoutOrientation.Vertical:
                            LyricsCanvas.LyricsStartX = LeftGapDef.ActualWidth;
                            LyricsCanvas.LyricsStartY = 0;
                            LyricsCanvas.LyricsWidth = TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth + LyricsColDef.ActualWidth;
                            LyricsCanvas.LyricsHeight = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight + LyricsRowDef.ActualHeight;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            if (LyricsWindowStatus.LyricsEffectSettings.Lyrics3DAutoFitLayout)
            {
                (LyricsCanvas.LyricsHeight, LyricsCanvas.LyricsWidth) = (LyricsCanvas.LyricsWidth, LyricsCanvas.LyricsHeight);
                LyricsCanvas.LyricsStartX += (LyricsCanvas.LyricsHeight - LyricsCanvas.LyricsWidth) / 2;
                LyricsCanvas.LyricsStartY += (LyricsCanvas.LyricsWidth - LyricsCanvas.LyricsHeight) / 2;
            }
        }

        // ====

        private void UpdateGap()
        {
            if (LyricsWindowStatus == null) return;

            var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);

            double height = RootGrid.ActualHeight;
            double width = RootGrid.ActualWidth;

            double minSize = Math.Min(width, height);

            double gapBetweenTrackSummaryAndLyrics = 0;
            double gapBetweenAlbumArtAndSongInfo = 0;
            double trackSummaryRowHeight = 0;

            double xMargin = 0;
            double yMargin = 0;

            switch (LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    if (width < 800)
                    {
                        xMargin = Math.Clamp(minSize * 0.15, 16, 128);
                    }
                    else
                    {
                        xMargin = Math.Clamp(minSize * 0.25, 16, 128);
                    }
                    yMargin = Math.Max(32, Math.Min(width, height) * 0.15);
                    if (height < 100)
                    {
                        gapBetweenTrackSummaryAndLyrics = Math.Max(16, height * 0.1);
                    }
                    else
                    {
                        gapBetweenTrackSummaryAndLyrics = 0;
                    }
                    TrackSummaryGridCol0.Width = new(1, GridUnitType.Star);
                    switch (LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.AlbumArtOnly:
                            TrackSummaryGridCol2.Width = new(0);
                            break;
                        case LyricsDisplayType.LyricsOnly:
                        case LyricsDisplayType.SplitView:
                            TrackSummaryGridCol2.Width = new(xMargin);
                            break;
                        default:
                            break;
                    }
                    TrackSummaryGridRow1.Height =
                        LyricsWindowStatus.AlbumArtLayoutSettings.IsAutoCoverImageHeight ? new(1, GridUnitType.Star) : new(LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageHeight);
                    break;
                case LyricsLayoutOrientation.Vertical:
                    xMargin = Math.Max(16, minSize * 0.05);
                    yMargin = Math.Max(16, minSize * 0.05);
                    trackSummaryRowHeight =
                        LyricsWindowStatus.AlbumArtLayoutSettings.IsAutoCoverImageHeight ? Math.Max(64, minSize * 0.25) : LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageHeight;
                    gapBetweenTrackSummaryAndLyrics = Math.Max(16, width * 0.15);
                    TrackSummaryGridCol0.Width = new(1, GridUnitType.Auto);
                    switch (LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.AlbumArtOnly:
                            TrackSummaryGridCol2.Width = new(0);
                            break;
                        case LyricsDisplayType.LyricsOnly:
                        case LyricsDisplayType.SplitView:
                            TrackSummaryGridCol2.Width = new(1, GridUnitType.Star);
                            break;
                        default:
                            break;
                    }
                    TrackSummaryGridRow1.Height = new(1, GridUnitType.Star);
                    break;
                default:
                    break;
            }

            if (height < 100)
            {
                gapBetweenAlbumArtAndSongInfo = 0;
            }
            else
            {
                gapBetweenAlbumArtAndSongInfo = lyricsLayoutMetrics.SongTitleSize / 2;
            }

            MiddleGapColDef.Width = new(gapBetweenTrackSummaryAndLyrics);

            TrackSummaryGridRow0.Height = new(yMargin);
            TrackSummaryGridRow4.Height = new(yMargin);
            LeftGapDef.Width = RightGapDef.Width = new(xMargin);

            TrackSummaryRowDef.Height = new(trackSummaryRowHeight);

            TrackSummaryGridCol1.Width = TrackSummaryGridRow2.Height = new(gapBetweenAlbumArtAndSongInfo);
        }

        private void OnLayoutChanged()
        {
            _layoutChangedTimer?.Debounce(() =>
            {
                UpdateGap();

                UpdateSongInfoOpacity();
                UpdateLyricsOpacity();
                UpdateAlbumArtOpacity();

                UpdateTrackSummaryGridSpan();
                UpdateAlbumArtGridSpan();
                UpdateSongInfoStackPanelSpan();
                UpdateLyricsPlaceholderSpan();

                UpdateLyricsLayout();
            }, Constants.Time.DebounceTimeout);
        }

        // ====

        private void UpdateAutoScrollViewIsPlaying(AutoScrollView element, bool isPointerEntered)
        {
            if (LyricsWindowStatus?.AlbumArtAreaEffectSettings.SongInfoAutoScroll == true)
            {
                element.IsPlaying = true;
            }
            else
            {
                element.IsPlaying = isPointerEntered;
            }
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ = RenderSongInfoAsync();
            OnLayoutChanged();
        }

        private void TitleAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
        }

        private void TitleAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, true);
        }

        private void TitleAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
        }

        private void ArtistsAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
        }

        private void ArtistsAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, true);
        }

        private void ArtistsAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
        }

        private void AlbumAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
        }

        private void AlbumAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, true);
        }

        private void AlbumAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
        }

        private void LyricsPlaceholder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnLayoutChanged();
        }

        private void LyricsScrollViewer_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseScrolling = true;

            var pointerPoint = e.GetCurrentPoint(LyricsScrollViewer);
            int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

            var value = LyricsCanvas.MouseScrollOffset + mouseWheelDelta;
            // 阻止向上滚动超过歌词最大边界
            if (value > 0)
            {
                value = Math.Min(-LyricsCanvas.CurrentCanvasYScroll, value);
            }
            // 阻止向下滚动超过歌词最大边界
            else
            {
                value = Math.Max(-LyricsCanvas.CurrentCanvasYScroll - LyricsCanvas.ActualLyricsHeight, value);
            }
            LyricsCanvas.MouseScrollOffset = value;

            _scrollChangedTimer?.Debounce(() =>
            {
                LyricsCanvas.MouseScrollOffset = 0;
                LyricsCanvas.IsMouseScrolling = false;
            }, TimeSpan.FromSeconds(3));
        }

        private void LyricsScrollViewer_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(LyricsScrollViewer);

            LyricsCanvas.MousePosition = pointerPoint.Position;
        }

        private void LyricsScrollViewer_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMousePressing = false;
            _ = _gsmtcService.ChangeLyricsLineAsync(LyricsCanvas.CurrentHoveringLineIndex);
        }

        private void LyricsScrollViewer_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseInLyricsArea = false;
        }

        private void LyricsScrollViewer_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMouseInLyricsArea = true;
        }

        private void LyricsScrollViewer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            LyricsCanvas.IsMousePressing = true;
        }

        private async void SaveAlbumArtButton_Click(object sender, RoutedEventArgs e)
        {
            var sourceStream = ViewModel.MediaSessionsService.AlbumArtBitmapStream;
            if (sourceStream == null) return;

            var window = WindowHook.GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == LyricsWindowStatus);
            if (window == null) return;

            IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
            {
                { "PNG", new List<string>() { ".png" } },
                { "JPEG", new List<string>() { ".jpg", ".jpeg" } }
            };

            var file = await PickerHelper.PickSaveFileAsync(window, fileTypeChoices);

            if (file != null)
            {
                using (IRandomAccessStream destStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    sourceStream.Seek(0);
                    await RandomAccessStream.CopyAsync(sourceStream, destStream);
                    await destStream.FlushAsync();

                    GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
                }
            }

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);

            _layoutChangedTimer?.Stop();
            _layoutChangedTimer = null;

            _scrollChangedTimer?.Stop();
            _scrollChangedTimer = null;

            DataContext = null;
        }

        private void AlbumArtGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var transform = AlbumArtGrid.TransformToVisual(RootGrid);
            var localRect = new Windows.Foundation.Rect(0, 0, AlbumArtGrid.ActualWidth, AlbumArtGrid.ActualHeight);
            LyricsCanvas.AlbumArtRect = transform.TransformBounds(localRect);
        }

        // ====

        public void Receive(PropertyChangedMessage<SongInfo> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentSongInfo))
                {
                    _ = RefreshSongInfoAsync();
                    UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsLayoutOrientation))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsDisplayType))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.SongInfoFontSize))
                {
                    _ = RenderSongInfoAsync();
                }
                else if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.CoverImageHeight))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender == LyricsWindowStatus?.AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.IsAutoSongInfoFontSize))
                {
                    _ = RenderSongInfoAsync();
                }
                else if (message.PropertyName == nameof(AlbumArtAreaStyleSettings.IsAutoCoverImageHeight))
                {
                    OnLayoutChanged();
                }
            }
            else if (message.Sender == LyricsWindowStatus?.AlbumArtAreaEffectSettings)
            {
                if (message.PropertyName == nameof(AlbumArtAreaEffectSettings.SongInfoAutoScroll))
                {
                    UpdateAutoScrollViewIsPlaying(TitleAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(ArtistsAutoScrollHoverEffectView, false);
                    UpdateAutoScrollViewIsPlaying(AlbumAutoScrollHoverEffectView, false);
                }
            }
            else if (message.Sender == LyricsWindowStatus?.LyricsEffectSettings)
            {
                if (message.PropertyName == nameof(LyricsEffectSettings.Lyrics3DAutoFitLayout))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender == LyricsWindowStatus?.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    _ = RenderSongInfoAsync();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    _ = RenderSongInfoAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
        {
            if (message.Sender is LyricsSearchControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                {
                    _ = RefreshSongInfoAsync();
                }
            }
        }

    }
}
