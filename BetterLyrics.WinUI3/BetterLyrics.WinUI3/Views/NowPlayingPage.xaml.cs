// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingPage : Page,
        IRecipient<PropertyChangedMessage<SongInfo?>>
    {
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();

        private readonly DispatcherQueueTimer _layoutChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();
        private readonly DispatcherQueueTimer _scrollChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();

        public NowPlayingPageViewModel ViewModel => (NowPlayingPageViewModel)DataContext;

        public LyricsWindowStatus LyricsWindowStatus
        {
            get { return (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty); }
            set { SetValue(LyricsWindowStatusProperty, value); }
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(NowPlayingPage), new PropertyMetadata(default, OnDependencyPropertyChanged));

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

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<SongInfo?>>(this);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NowPlayingPage page)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    var oldValue = (LyricsWindowStatus?)e.OldValue;
                    oldValue?.PropertyChanged -= page.LyricsWindowStatus_PropertyChanged;
                    var newValue = (LyricsWindowStatus?)e.NewValue;
                    newValue?.PropertyChanged += page.LyricsWindowStatus_PropertyChanged;
                    page.OnLayoutChanged();
                    page.RenderSongInfo();
                }
                else if (e.Property == AlbumArtThemeColorsProperty)
                {
                    page.RenderSongInfo();
                }
            }
        }

        private void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnLayoutChanged();
            RenderSongInfo();
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            var currentTime = LyricsCanvas.SongPosition.TotalSeconds;
            TimelineSlider.Value = currentTime;
        }

        // ==== SongInfo

        private void RenderTextBlock(TextBlock? sender, string? text, double fontSize)
        {
            if (sender == null || text == null || fontSize == 0) return;

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

        private void RenderSongInfo()
        {
            var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);

            var albumArtLayoutSettings = LyricsWindowStatus.AlbumArtLayoutSettings;

            var titleFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.SongTitleSize : albumArtLayoutSettings.SongInfoFontSize;
            var artistsFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.ArtistNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;
            var albumFontSize = albumArtLayoutSettings.IsAutoSongInfoFontSize ? lyricsLayoutMetrics.AlbumNameSize : albumArtLayoutSettings.SongInfoFontSize * 0.8;

            RenderTextBlock(TitleTextBlock, _mediaSessionsService.CurrentSongInfo?.Title, titleFontSize);
            RenderTextBlock(ArtistsTextBlock, _mediaSessionsService.CurrentSongInfo?.DisplayArtists, artistsFontSize);
            RenderTextBlock(AlbumTextBlock, _mediaSessionsService.CurrentSongInfo?.Album, albumFontSize);
        }

        private void UpdateSongInfoOpacity()
        {
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

        // ==== AlbumArt
        private void UpdateAlbumArtOpacity()
        {
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
            var status = LyricsWindowStatus;
            switch (status.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    Grid.SetRowSpan(TrackSummaryGrid, 3);
                    Grid.SetColumnSpan(TrackSummaryGrid, 3);
                    break;
                case LyricsDisplayType.LyricsOnly:
                    break;
                case LyricsDisplayType.SplitView:
                    switch (status.LyricsLayoutOrientation)
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
            var status = LyricsWindowStatus;
            switch (status.LyricsLayoutOrientation)
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
            var status = LyricsWindowStatus;
            switch (status.LyricsDisplayType)
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
                    switch (status.LyricsLayoutOrientation)
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
            var status = LyricsWindowStatus;
            switch (status.LyricsLayoutOrientation)
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
            var status = LyricsWindowStatus;
            switch (status.LyricsDisplayType)
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
                    switch (status.LyricsLayoutOrientation)
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
        }

        // ====

        private void UpdateGap()
        {
            var lyricsLayoutMetrics = LyricsLayoutHelper.CalculateLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);

            var status = LyricsWindowStatus;

            double height = RootGrid.ActualHeight;
            double width = RootGrid.ActualWidth;

            double minSize = Math.Min(width, height);

            double gapBetweenTrackSummaryAndLyrics = 0;
            double gapBetweenAlbumArtAndSongInfo = 0;
            double trackSummaryRowHeight = 0;

            double xMargin = 0;
            double yMargin = 0;

            switch (status.LyricsLayoutOrientation)
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
                    TrackSummaryGridCol2.Width = new(xMargin);
                    TrackSummaryGridRow1.Height =
                        status.AlbumArtLayoutSettings.IsAutoCoverImageHeight ? new(1, GridUnitType.Star) : new(status.AlbumArtLayoutSettings.CoverImageHeight);
                    break;
                case LyricsLayoutOrientation.Vertical:
                    xMargin = Math.Max(16, minSize * 0.05);
                    yMargin = Math.Max(16, minSize * 0.05);
                    trackSummaryRowHeight =
                        status.AlbumArtLayoutSettings.IsAutoCoverImageHeight ? Math.Max(64, minSize * 0.25) : status.AlbumArtLayoutSettings.CoverImageHeight;
                    gapBetweenTrackSummaryAndLyrics = Math.Max(16, width * 0.15);
                    TrackSummaryGridCol0.Width = new(1, GridUnitType.Auto);
                    TrackSummaryGridCol2.Width = new(1, GridUnitType.Star);
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
            _layoutChangedTimer.Debounce(() =>
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

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = e.NewSize.Width;
            var height = e.NewSize.Height;

            RenderSongInfo();
            OnLayoutChanged();

            if (width < 500 || height < 100)
            {
                if (BottomCommandGrid.Children.Count != 0)
                {
                    BottomCommandGrid.Children.Remove(BottomCommandContent);
                    BottomCommandFlyoutContainer.Children.Add(BottomCommandContent);
                }
                BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 0, 0);
            }
            else
            {
                if (BottomCommandFlyoutContainer.Children.Count != 0)
                {
                    BottomCommandFlyout.Hide();
                    BottomCommandFlyoutContainer.Children.Remove(BottomCommandContent);
                    BottomCommandGrid.Children.Add(BottomCommandContent);
                }
                BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 12, 0);
            }
        }

        private void BottomCommandGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (BottomCommandGrid.Children.Count != 0)
            {
                ViewModel.BottomCommandGridOpacity = 1f;
            }
            e.Handled = true;
        }

        private void BottomCommandGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (BottomCommandGrid.Children.Count != 0)
            {
                ViewModel.BottomCommandGridOpacity = 0f;
            }
            e.Handled = true;
        }

        private void PlaybackSettingsShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsFlyout.Content = new PlaybackSettingsControl
            {
                MaxHeight = 500,
                MaxWidth = 850,
            };
            PlaybackSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            VolumeFlyout.ShowAt(BottomLeftCommandStackPanel);
        }

        private void BottomCommandFlyoutTrigger_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                ViewModel.BottomCommandFlyoutTriggerOpacity = 1f;
            }
        }

        private void BottomCommandFlyoutTrigger_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                ViewModel.BottomCommandFlyoutTriggerOpacity = 0f;
            }
        }

        private void BottomCommandFlyoutTrigger_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                BottomCommandFlyout.ShowAt(BottomCommandFlyoutTrigger);
            }
        }

        private void PlaybackSettingsFlyout_Closed(object sender, object e)
        {
            PlaybackSettingsFlyout.Content = null;
        }

        private void LyricsSettingsFlyout_Closed(object sender, object e)
        {
            LyricsSettingsFlyout.Content = null;
        }

        private void LyricsSettingsShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsSettingsFlyout.Content = new LyricsWindowSettingsControl
            {
                MaxHeight = 500,
                MaxWidth = 850,
            };
            LyricsSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void LyricsSearchShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<LyricsSearchWindow>();
        }

        private void TimelineSliderOverlay_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var grid = (Grid)sender;
            var pos = e.GetCurrentPoint(grid).Position;
            var ratio = pos.X / grid.ActualWidth;
            _mediaSessionsService.ChangePosition(TimelineSlider.Maximum * ratio);
        }

        private void TimelineSliderOverlay_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            float targetX;
            var grid = (Grid)sender;
            var pos = e.GetCurrentPoint(grid).Position;
            var ratio = pos.X / grid.ActualWidth;
            ViewModel.TimelineSliderThumbSeconds = TimelineSlider.Maximum * ratio;
            if (pos.X + TimelineSliderLyricsLineInfo.ActualWidth > grid.ActualWidth)
            {
                targetX = (float)(grid.ActualWidth - TimelineSliderLyricsLineInfo.ActualWidth);
            }
            else
            {
                targetX = (float)pos.X;
            }
            TimelineSliderLyricsLineInfo.Translation = new Vector3(targetX, 0, 0);
        }

        private void TimelineSliderOverlay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ViewModel.TimelineSliderThumbOpacity = 0.7f;
        }

        private void TimelineSliderOverlay_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ViewModel.TimelineSliderThumbOpacity = 0f;
        }

        private void RootGrid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                BottomCommandFlyout.ShowAt(BottomCommandFlyoutTrigger);
            }
        }

        private void ExtendedSlider_ValueChangedByUser(object sender, Events.ExtendedSliderValueChangedByUserEventArgs e)
        {
            SystemVolumeHook.MasterVolume = ViewModel.Volume;
        }

        private void TitleAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            TitleAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void TitleAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            TitleAutoScrollHoverEffectView.IsPlaying = true;
        }

        private void TitleAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            TitleAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void ArtistsAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ArtistsAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void ArtistsAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ArtistsAutoScrollHoverEffectView.IsPlaying = true;
        }

        private void ArtistsAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ArtistsAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void AlbumAutoScrollHoverEffectView_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AlbumAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void AlbumAutoScrollHoverEffectView_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AlbumAutoScrollHoverEffectView.IsPlaying = true;
        }

        private void AlbumAutoScrollHoverEffectView_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AlbumAutoScrollHoverEffectView.IsPlaying = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
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

            _scrollChangedTimer.Debounce(() =>
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
            _mediaSessionsService.ChangeLyricsLine(LyricsCanvas.CurrentHoveringLineIndex);
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

        // ====

        public async void Receive(PropertyChangedMessage<SongInfo?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentSongInfo))
                {
                    SongInfoStackPanel.Opacity = 0;
                    await Task.Delay(Constants.Time.AnimationDuration);
                    RenderSongInfo();
                    SongInfoStackPanel.Opacity = 1;
                    UpdateSongInfoOpacity();
                }
            }
        }

    }
}
