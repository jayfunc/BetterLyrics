// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
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
using System.Numerics;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsPage : Page,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<SongInfo?>>
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();

        private double _leftMargin = 36f;
        private double _middleMargin = 36f;
        private double _rightMargin = 36f;
        private double _topMargin = 36f;
        private double _bottomMargin = 36f;

        private readonly DispatcherQueueTimer _rootGridSizeChangedTimer;

        public double AlbumArtSize
        {
            get { return (double)GetValue(AlbumArtSizeProperty); }
            set { SetValue(AlbumArtSizeProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtSizeProperty =
            DependencyProperty.Register(nameof(AlbumArtSize), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(0.0));

        public CornerRadius AlbumArtCornerRadius
        {
            get { return (CornerRadius)GetValue(AlbumArtCornerRadiusProperty); }
            set { SetValue(AlbumArtCornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty AlbumArtCornerRadiusProperty =
            DependencyProperty.Register(nameof(AlbumArtCornerRadius), typeof(double), typeof(NowPlayingCanvas), new PropertyMetadata(new CornerRadius(0)));

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<LyricsPageViewModel>();

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<int>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<string>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<SongInfo?>>(this);

            _rootGridSizeChangedTimer = App.Current.Resources.DispatcherQueue.CreateTimer();
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            var currentTime = NowPlayingCanvas.SongPosition.TotalSeconds;
            TimelineSlider.Value = currentTime;
        }

        private void RenderTextBlock(TextBlock? sender, string? text, int fontSize)
        {
            if (sender == null || string.IsNullOrEmpty(text) || fontSize == 0) return;

            var lyricsStyleSettings = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings;

            sender.Inlines.Clear();
            foreach (var ch in text)
            {
                var fontFamilyName = LanguageHelper.IsCJK(ch) ? lyricsStyleSettings.LyricsCJKFontFamily : lyricsStyleSettings.LyricsWesternFontFamily;
                sender.Inlines.Add(new Run { Text = $"{ch}", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(fontFamilyName) });
            }
            sender.FontSize = fontSize;
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

        private void UpdateAlbumArtSize()
        {
            var lyricsWindowStatus = _liveStatesService.LiveStates.LyricsWindowStatus;
            var albumArtLayoutSettings = lyricsWindowStatus.AlbumArtLayoutSettings;
            double temp = 0;
            switch (lyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    if (albumArtLayoutSettings.AutoAlbumArtSize)
                    {
                        temp = Math.Min(
                            (RootGrid.ActualHeight - _topMargin - _bottomMargin) * 8.5 / 16.0,
                            (RootGrid.ActualWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0);
                    }
                    else
                    {
                        temp = albumArtLayoutSettings.AlbumArtSize;
                    }
                    break;
                case LyricsLayoutOrientation.Vertical:
                    if (albumArtLayoutSettings.AutoAlbumArtSize)
                    {
                        temp = Math.Min(
                            (RootGrid.ActualHeight - _topMargin - _bottomMargin) * 3.0 / 16.0,
                            (RootGrid.ActualWidth - _leftMargin - _middleMargin - _rightMargin) * 4.0 / 16.0);
                    }
                    else
                    {
                        temp = albumArtLayoutSettings.AlbumArtSize;
                    }
                    break;
            }

            AlbumArtSize = Math.Max(0, temp);
        }

        private void UpdateAlbumArtCornerRadius()
        {
            var factor = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageRadius / 100.0;
            AlbumArtCornerRadius = new((AlbumArtSize / 2) * factor);
        }

        private double GetAlbumArtY()
        {
            double temp = 0;
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    temp = (RootGrid.ActualHeight - AlbumArtWithSongInfoStackPanel.ActualHeight) / 2.0;
                    break;
                case LyricsLayoutOrientation.Vertical:
                    switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.AlbumArtOnly:
                            temp = (RootGrid.ActualHeight - AlbumArtSize) / 2.0;
                            break;
                        case LyricsDisplayType.SplitView:
                            temp = _topMargin;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return (float)temp - 32;
        }

        private double GetAlbumArtX()
        {
            double temp = 0;
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.AlbumArtOnly:
                            temp = RootGrid.ActualWidth / 2.0 - AlbumArtSize / 2.0;
                            break;
                        case LyricsDisplayType.SplitView:
                            temp = _leftMargin + ((RootGrid.ActualWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 - AlbumArtSize) / 2.0;
                            break;
                        default:
                            break;
                    }
                    break;
                case LyricsLayoutOrientation.Vertical:
                    temp = _leftMargin;
                    break;
                default:
                    break;
            }

            return (float)temp - 32;
        }

        private void UpdateAlbumArtTranslation()
        {
            var x = GetAlbumArtX();
            var y = GetAlbumArtY();
            AlbumArtWithSongInfoStackPanel.Translation = new((float)x, (float)y, 0);
        }

        private void UpdateMargin()
        {
            _topMargin = _bottomMargin = _leftMargin = _middleMargin = _rightMargin = Math.Max(RootGrid.ActualWidth, RootGrid.ActualHeight) / 30.0;
        }

        private void UpdateLyricsStartY()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    NowPlayingCanvas.LyricsStartY = 0;
                    break;
                case LyricsLayoutOrientation.Vertical:
                    switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.LyricsOnly:
                            NowPlayingCanvas.LyricsStartY = 0;
                            break;
                        case LyricsDisplayType.SplitView:
                            NowPlayingCanvas.LyricsStartY = _topMargin;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

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

        private void UpdateLyricsStartX()
        {
            switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsLayoutOrientation)
            {
                case LyricsLayoutOrientation.Horizontal:
                    switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsDisplayType)
                    {
                        case LyricsDisplayType.LyricsOnly:
                            NowPlayingCanvas.LyricsStartX = _leftMargin;
                            break;
                        case LyricsDisplayType.SplitView:
                            NowPlayingCanvas.LyricsStartX = (RootGrid.ActualWidth - _leftMargin - _middleMargin - _rightMargin) / 2.0 + _leftMargin + _middleMargin;
                            break;
                        default:
                            break;
                    }
                    break;
                case LyricsLayoutOrientation.Vertical:
                    NowPlayingCanvas.LyricsStartX = _leftMargin;
                    break;
                default:
                    break;
            }
        }

        private void UpdateLyricsWidth()
        {
            NowPlayingCanvas.LyricsWidth = Math.Max(RootGrid.ActualWidth - NowPlayingCanvas.LyricsStartX - _rightMargin, 0);
        }

        private void OnLayoutChanged()
        {
            UpdateMargin();

            UpdateAlbumArtSize();
            UpdateAlbumArtCornerRadius();
            UpdateAlbumArtTranslation();

            UpdateLyricsStartX();
            UpdateLyricsStartY();

            UpdateLyricsWidth();
        }

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

        // ====

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _rootGridSizeChangedTimer.Debounce(() =>
            {
                RenderSongInfo();

                OnLayoutChanged();

                if (e.NewSize.Width < 500 || e.NewSize.Height < 100)
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
            }, Constants.Time.DebounceTimeout);
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

        private void AlbumArtWithSongInfoStackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.AlbumArtWithSongInfoStackPanelHeight = e.NewSize.Height;
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

        // ====

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is AlbumArtLayoutSettings)
            {
                if (message.PropertyName == nameof(AlbumArtLayoutSettings.SongInfoFontSize))
                {
                    RenderSongInfo();
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

    }
}
