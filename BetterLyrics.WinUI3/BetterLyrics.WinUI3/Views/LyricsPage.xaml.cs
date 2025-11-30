// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsPage : Page,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<SongInfo?>>,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<LyricsLayoutOrientation>>,
        IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
        IRecipient<PropertyChangedMessage<AlbumArtThemeColors>>,
        IRecipient<PropertyChangedMessage<LyricsWindowStatus>>
    {
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();

        public CornerRadius AlbumArtCornerRadius
        {
            get { return (CornerRadius)GetValue(AlbumArtCornerRadiusProperty); }
            set { SetValue(AlbumArtCornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtCornerRadiusProperty =
            DependencyProperty.Register(nameof(AlbumArtCornerRadius), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(new CornerRadius(0)));


        public double AlbumArtSize
        {
            get { return (double)GetValue(AlbumArtSizeProperty); }
            set { SetValue(AlbumArtSizeProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtSizeProperty =
            DependencyProperty.Register(nameof(AlbumArtSize), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0));

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<LyricsPageViewModel>();

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<int>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<string>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<SongInfo?>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<BitmapImage?>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsLayoutOrientation>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsDisplayType>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<AlbumArtThemeColors>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<LyricsWindowStatus>>(this);
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            var currentTime = NowPlayingCanvas.SongPosition.TotalSeconds;
            TimelineSlider.Value = currentTime;
        }

        // ==== SongInfo

        private int GetTitleFontSize()
        {
            var albumArtLayoutSettings = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings;
            if (albumArtLayoutSettings.IsAutoSongInfoFontSize)
            {
                return (int)Math.Clamp(Math.Min(RootGrid.ActualHeight, RootGrid.ActualWidth) / 20, 8, 72);
            }
            else
            {
                return albumArtLayoutSettings.SongInfoFontSize;
            }
        }

        private int GetArtistsAlbumFontSize()
        {
            return (int)(GetTitleFontSize() * 0.8);
        }

        private void RenderTextBlock(TextBlock? sender, string? text, int fontSize)
        {
            if (sender == null || string.IsNullOrEmpty(text) || fontSize == 0) return;

            var lyricsStyleSettings = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings;

            sender.Inlines.Clear();
            foreach (var ch in text)
            {
                var fontFamilyName = LanguageHelper.IsCJK(ch) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;
                sender.Inlines.Add(new Run { Text = $"{ch}", FontFamily = new FontFamily(fontFamilyName) });
            }
            sender.FontSize = fontSize;
            sender.Foreground = new SolidColorBrush(_mediaSessionsService.AlbumArtThemeColors.BgFontColor);
        }

        private void RenderTextBlock(AnimatedTextBlock? sender, string? text, int fontSize)
        {
            if (sender == null || string.IsNullOrEmpty(text) || fontSize == 0) return;

            var lyricsStyleSettings = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings;
            var fontFamilyName = LanguageHelper.IsCJK(text) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;

            sender.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fontFamilyName);
            sender.FontSize = fontSize;

            sender.Text = text;
        }

        private void RenderSongInfo()
        {
            RenderTextBlock(TitleTextBlock, _mediaSessionsService.CurrentSongInfo?.Title, GetTitleFontSize());
            RenderTextBlock(ArtistsTextBlock, _mediaSessionsService.CurrentSongInfo?.DisplayArtists, GetArtistsAlbumFontSize());
            RenderTextBlock(AlbumTextBlock, _mediaSessionsService.CurrentSongInfo?.Album, GetArtistsAlbumFontSize());
        }

        private void UpdateSongInfoOpacity()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
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

        private void UpdateAlbumArtCornerRadius()
        {
            var factor = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageRadius / 100.0;
            AlbumArtCornerRadius = new((AlbumArtImage.ActualHeight / 2) * factor);
        }

        private void UpdateAlbumArtShadow()
        {
            var amount = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageShadowAmount;
            ShadowRect.Translation = new(0, 0, amount);
        }

        private void UpdateAlbumArtOpacity()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
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
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
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
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            switch (status.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    Grid.SetRow(SongInfoStackPanel, 3);
                    Grid.SetRowSpan(SongInfoStackPanel, 1);
                    Grid.SetColumn(SongInfoStackPanel, 0);
                    Grid.SetColumnSpan(SongInfoStackPanel, 3);
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

        // ====

        private void UpdateAlbumArtGridSpan()
        {
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            switch (status.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    Grid.SetRow(AlbumArtGrid, 1);
                    Grid.SetRowSpan(AlbumArtGrid, 1);
                    Grid.SetColumn(AlbumArtGrid, 0);
                    Grid.SetColumnSpan(AlbumArtGrid, 3);
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
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    NowPlayingCanvas.LyricsOpacity = 0;
                    break;
                case LyricsDisplayType.LyricsOnly:
                case LyricsDisplayType.SplitView:
                    NowPlayingCanvas.LyricsOpacity = 1;
                    break;
                default:
                    break;
            }
        }

        private void UpdateLyricsLayout()
        {
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            switch (status.LyricsDisplayType)
            {
                case LyricsDisplayType.AlbumArtOnly:
                    break;
                case LyricsDisplayType.LyricsOnly:
                    NowPlayingCanvas.LyricsStartX = LeftGapDef.ActualWidth;
                    NowPlayingCanvas.LyricsStartY = 0;
                    NowPlayingCanvas.LyricsWidth = TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth + LyricsColDef.ActualWidth;
                    NowPlayingCanvas.LyricsHeight = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight + LyricsRowDef.ActualHeight;
                    break;
                case LyricsDisplayType.SplitView:
                    switch (status.LyricsLayoutOrientation)
                    {
                        case LyricsLayoutOrientation.Horizontal:
                            NowPlayingCanvas.LyricsStartX = LeftGapDef.ActualWidth + TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth;
                            NowPlayingCanvas.LyricsStartY = 0;
                            NowPlayingCanvas.LyricsWidth = LyricsColDef.ActualWidth;
                            NowPlayingCanvas.LyricsHeight = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight + LyricsRowDef.ActualHeight;
                            break;
                        case LyricsLayoutOrientation.Vertical:
                            NowPlayingCanvas.LyricsStartX = LeftGapDef.ActualWidth;
                            NowPlayingCanvas.LyricsStartY = TrackSummaryRowDef.ActualHeight + MiddleGapRowDef.ActualHeight;
                            NowPlayingCanvas.LyricsWidth = TrackSummaryColDef.ActualWidth + MiddleGapColDef.ActualWidth + LyricsColDef.ActualWidth;
                            NowPlayingCanvas.LyricsHeight = LyricsRowDef.ActualHeight;
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
            var status = _liveStatesService.LiveStates.LyricsWindowStatus;

            double height = RootGrid.ActualHeight;
            double width = RootGrid.ActualWidth;

            double xMargin = 0;
            double yMargin = 0;
            double middleGapCol = 0;
            double gapBetweenAlbumArtAndSongInfo = 0;

            if (height < 400)
            {
                middleGapCol = Math.Max(16, height * 0.1);
            }
            else
            {
                middleGapCol = Math.Max(16, width * 0.1);
            }

            if (height < 100)
            {
                gapBetweenAlbumArtAndSongInfo = 0;
            }
            else
            {
                gapBetweenAlbumArtAndSongInfo = GetTitleFontSize() / 2;
            }

            switch (status.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    xMargin = Math.Max(16, Math.Min(width, height) * 0.3);
                    yMargin = Math.Max(16, Math.Min(width, height) * 0.15);
                    break;
                case LyricsLayoutOrientation.Vertical:
                    xMargin = Math.Max(16, Math.Min(width, height) * 0.05);
                    yMargin = xMargin;
                    break;
                default:
                    break;
            }

            MiddleGapColDef.Width = new(middleGapCol);

            TrackSummaryGridRow0.Height = TrackSummaryGridRow4.Height = new(yMargin);
            LeftGapDef.Width = RightGapDef.Width = new(xMargin);
            TrackSummaryGridCol1.Width = TrackSummaryGridRow2.Height = new(gapBetweenAlbumArtAndSongInfo);
        }

        private void OnLayoutChanged()
        {
            UpdateSongInfoOpacity();

            UpdateAlbumArtShadow();
            UpdateAlbumArtOpacity();

            UpdateTrackSummaryGridSpan();

            UpdateAlbumArtGridSpan();

            UpdateSongInfoStackPanelSpan();

            UpdateLyricsOpacity();
            UpdateLyricsLayout();

            UpdateAlbumArtCornerRadius();

            UpdateGap();
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

        private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
        {
            Shadow.Receivers.Add(ShadowCastGrid);
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

        private void TrackSummaryGridContainer_Loaded(object sender, RoutedEventArgs e)
        {
            OnLayoutChanged();
        }

        private void TrackSummaryGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //var status = _liveStatesService.LiveStates.LyricsWindowStatus;
            //switch (status.LyricsLayoutOrientation)
            //{
            //    case LyricsLayoutOrientation.Horizontal:
            //        TitleAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth;
            //        ArtistsAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth;
            //        AlbumAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth;
            //        break;
            //    case LyricsLayoutOrientation.Vertical:
            //        TitleAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth * 3;
            //        ArtistsAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth * 3;
            //        AlbumAutoScrollHoverEffectView.Width = AlbumArtImage.ActualWidth * 3;
            //        break;
            //    default:
            //        break;
            //}
        }

        // ====

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.SongInfoFontSize))
                {
                    RenderSongInfo();
                }
                else if (message.PropertyName == nameof(AlbumArtLayoutSettings.CoverImageRadius))
                {
                    UpdateAlbumArtCornerRadius();
                }
                else if (message.PropertyName == nameof(AlbumArtLayoutSettings.CoverImageShadowAmount))
                {
                    UpdateAlbumArtShadow();
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.IsAutoSongInfoFontSize))
                {
                    RenderSongInfo();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsCJKFontFamily))
                {
                    RenderSongInfo();
                }
                else if (message.PropertyName == nameof(LyricsStyleSettings.LyricsWesternFontFamily))
                {
                    RenderSongInfo();
                }
            }
        }

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
                }
            }
        }

        public async void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapImage))
                {
                    LastAlbumArtImage.Source = AlbumArtImage.Source;
                    LastAlbumArtImage.Opacity = 1;
                    await Task.Delay(Constants.Time.AnimationDuration);

                    AlbumArtImage.Opacity = 0;
                    await Task.Delay(Constants.Time.AnimationDuration);
                    AlbumArtImage.Source = message.NewValue;

                    LastAlbumArtImage.Opacity = 0;
                    AlbumArtImage.Opacity = 1;

                    UpdateAlbumArtCornerRadius();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsLayoutOrientation> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsLayoutOrientation))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            if (message.Sender is LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsDisplayType))
                {
                    OnLayoutChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<AlbumArtThemeColors> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtThemeColors))
                {
                    RenderSongInfo();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsWindowStatus> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsWindowStatus))
                {
                    OnLayoutChanged();
                }
            }
        }

        private void ShadowCastGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAlbumArtCornerRadius();
        }
    }
}
