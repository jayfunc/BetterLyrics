using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Media.Control;
using Color = Windows.UI.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;
        private SettingsService SettingsService { get; set; }

        private readonly CoverBackgroundRenderer _coverImageAsBackgroundRenderer = new();
        private readonly PureLyricsRenderer _pureLyricsRenderer = new();

        private readonly float _coverRotateSpeed = 0.003f;

        private float _lyricsGlowEffectAngle = 0f;
        private readonly float _lyricsGlowEffectSpeed = 0.01f;

        private readonly float _lyricsGlowEffectMinBlurAmount = 0f;
        private readonly float _lyricsGlowEffectMaxBlurAmount = 6f;

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        private Color _lyricsColor;

        private readonly ILogger<MainPage> _logger;

        public MainPage()
        {
            this.InitializeComponent();

            _logger = Ioc.Default.GetService<ILogger<MainPage>>()!;
            SettingsService = Ioc.Default.GetService<SettingsService>()!;
            DataContext = Ioc.Default.GetService<MainViewModel>();

            SettingsService.PropertyChanged += SettingsService_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            SetLyricsColor();

            if (SettingsService.IsFirstRun)
            {
                WelcomeTeachingTip.IsOpen = true;
            }
        }

        private async void SettingsService_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsService.LyricsFontSize):
                case nameof(SettingsService.LyricsLineSpacingFactor):
                    await _pureLyricsRenderer.ReLayoutAsync(LyricsCanvas);
                    break;
                case nameof(SettingsService.IsRebuildingLyricsIndexDatabase):
                    if (!SettingsService.IsRebuildingLyricsIndexDatabase)
                    {
                        CurrentSession_MediaPropertiesChanged(_currentSession, null);
                    }
                    break;
                case nameof(SettingsService.Theme):
                case nameof(SettingsService.LyricsFontColorType):
                case nameof(SettingsService.LyricsFontSelectedAccentColorIndex):
                    await Task.Delay(1);
                    SetLyricsColor();
                    break;
                case nameof(SettingsService.CoverImageRadius):
                    CoverImageGrid.CornerRadius = new CornerRadius(
                        SettingsService.CoverImageRadius / 100f * (CoverImageGrid.ActualHeight / 2)
                    );
                    break;
                case nameof(SettingsService.IsImmersiveMode):
                    if (SettingsService.IsImmersiveMode)
                        WeakReferenceMessenger.Default.Send(
                            new ShowNotificatonMessage(
                                new Notification(
                                    App.ResourceLoader!.GetString("MainPageEnterImmersiveModeHint"),
                                    isForeverDismissable: true,
                                    relatedSettingsKeyName: SettingsKeys.NeverShowEnterImmersiveModeMessage
                                )
                            )
                        );
                    break;
                default:
                    break;
            }
        }

        private async void ViewModel_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ShowLyricsOnly):
                    if (ViewModel.ShowLyricsOnly)
                    {
                        Grid.SetColumn(LyricsPlaceholderGrid, 0);
                        Grid.SetColumnSpan(LyricsPlaceholderGrid, 3);
                    }
                    else
                    {
                        Grid.SetColumn(LyricsPlaceholderGrid, 2);
                        Grid.SetColumnSpan(LyricsPlaceholderGrid, 1);
                    }
                    await _pureLyricsRenderer.ReLayoutAsync(LyricsCanvas);
                    break;
                default:
                    break;
            }
        }

        private void SetLyricsColor()
        {
            switch ((LyricsFontColorType)SettingsService.LyricsFontColorType)
            {
                case LyricsFontColorType.Default:
                    _lyricsColor = ((SolidColorBrush)LyricsCanvas.Foreground).Color;
                    break;
                case LyricsFontColorType.Dominant:
                    _lyricsColor = ViewModel.CoverImageDominantColors[
                        Math.Max(
                            0,
                            Math.Min(
                                ViewModel.CoverImageDominantColors.Count - 1,
                                SettingsService.LyricsFontSelectedAccentColorIndex
                            )
                        )
                    ];
                    break;
                default:
                    break;
            }
        }

        private async void InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
            _sessionManager.SessionsChanged += SessionManager_SessionsChanged;

            SessionManager_CurrentSessionChanged(_sessionManager, null);
        }

        private void CurrentSession_TimelinePropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            TimelinePropertiesChangedEventArgs? args
        )
        {
            if (sender == null)
            {
                _pureLyricsRenderer.CurrentTime = TimeSpan.Zero;
                return;
            }

            _pureLyricsRenderer.CurrentTime = sender.GetTimelineProperties().Position;
            // _logger.LogDebug(_currentTime);
        }

        /// <summary>
        /// Note: Non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_PlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            PlaybackInfoChangedEventArgs? args
        )
        {
            App.DispatcherQueue!.TryEnqueue(
                DispatcherQueuePriority.Normal,
                () =>
                {
                    if (sender == null)
                    {
                        LyricsCanvas.Paused = true;
                        return;
                    }

                    var playbackState = sender.GetPlaybackInfo().PlaybackStatus;
                    // _logger.LogDebug(playbackState.ToString());

                    switch (playbackState)
                    {
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                            LyricsCanvas.Paused = true;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                            LyricsCanvas.Paused = false;
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                            LyricsCanvas.Paused = true;
                            break;
                        default:
                            break;
                    }
                }
            );
        }

        private void SessionManager_SessionsChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            SessionsChangedEventArgs? args
        )
        {
            // _logger.LogDebug("SessionManager_SessionsChanged");
        }

        private void SessionManager_CurrentSessionChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            CurrentSessionChangedEventArgs? args
        )
        {
            // _logger.LogDebug("SessionManager_CurrentSessionChanged");
            // Unregister events associated with the previous session
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -=
                    CurrentSession_TimelinePropertiesChanged;
            }

            // Record and register events for current session
            _currentSession = sender.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged +=
                    CurrentSession_TimelinePropertiesChanged;
            }

            CurrentSession_MediaPropertiesChanged(_currentSession, null);
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession? sender,
            MediaPropertiesChangedEventArgs? args
        )
        {
            App.DispatcherQueueTimer!.Debounce(
                () =>
                {
                    // _logger.LogDebug("CurrentSession_MediaPropertiesChanged");
                    App.DispatcherQueue!.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps =
                                null;

                            if (_currentSession != null)
                                mediaProps = await _currentSession.TryGetMediaPropertiesAsync();

                            ViewModel.IsAnyMusicSessionExisted = _currentSession != null;

                            ViewModel.AboutToUpdateUI = true;
                            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

                            (
                                _pureLyricsRenderer.LyricsLines,
                                _coverImageAsBackgroundRenderer.SoftwareBitmap
                            ) = await ViewModel.SetSongInfoAsync(mediaProps);

                            // Force to show lyrics and scroll to current line even if the music is not playing
                            LyricsCanvas.Paused = false;
                            await _pureLyricsRenderer.ForceToScrollToCurrentPlayingLineAsync();
                            await Task.Delay(1);
                            // Detect and recover the music state
                            CurrentSession_PlaybackInfoChanged(_currentSession, null);
                            CurrentSession_TimelinePropertiesChanged(_currentSession, null);

                            ViewModel.AboutToUpdateUI = false;

                            if (_pureLyricsRenderer.LyricsLines.Count == 0)
                            {
                                Grid.SetColumnSpan(SongInfoInnerGrid, 3);
                            }
                            else
                            {
                                Grid.SetColumnSpan(SongInfoInnerGrid, 1);
                            }
                        }
                    );
                },
                TimeSpan.FromMilliseconds(AnimationHelper.DebounceDefaultDuration)
            );
        }

        // Comsumes GPU related resources
        private void LyricsCanvas_Draw(
            ICanvasAnimatedControl sender,
            CanvasAnimatedDrawEventArgs args
        )
        {
            using var ds = args.DrawingSession;

            var r = _lyricsColor.R;
            var g = _lyricsColor.G;
            var b = _lyricsColor.B;

            // Draw (dynamic) cover image as the very first layer
            _coverImageAsBackgroundRenderer.Draw(sender, ds);

            // Lyrics only layer
            using var lyrics = new CanvasCommandList(sender);
            using (var lyricsDs = lyrics.CreateDrawingSession())
            {
                _pureLyricsRenderer.Draw(sender, lyricsDs, r, g, b);
            }

            using var glowedLyrics = new CanvasCommandList(sender);
            using (var glowedLyricsDs = glowedLyrics.CreateDrawingSession())
            {
                if (SettingsService.IsLyricsGlowEffectEnabled)
                {
                    glowedLyricsDs.DrawImage(
                        new GaussianBlurEffect
                        {
                            Source = lyrics,
                            BlurAmount =
                                MathF.Sin(_lyricsGlowEffectAngle)
                                    * (
                                        _lyricsGlowEffectMaxBlurAmount
                                        - _lyricsGlowEffectMinBlurAmount
                                    )
                                    / 2f
                                + (_lyricsGlowEffectMaxBlurAmount + _lyricsGlowEffectMinBlurAmount)
                                    / 2f,
                            BorderMode = EffectBorderMode.Soft,
                            Optimization = EffectOptimization.Quality,
                        }
                    );
                }
                glowedLyricsDs.DrawImage(lyrics);
            }

            // Mock gradient blurred lyrics layer
            using var combinedBlurredLyrics = new CanvasCommandList(sender);
            using var combinedBlurredLyricsDs = combinedBlurredLyrics.CreateDrawingSession();
            if (SettingsService.LyricsBlurAmount == 0)
            {
                combinedBlurredLyricsDs.DrawImage(glowedLyrics);
            }
            else
            {
                double step = 0.05;
                double overlapFactor = 0;
                for (double i = 0; i <= 0.5 - step; i += step)
                {
                    using var blurredLyrics = new GaussianBlurEffect
                    {
                        Source = glowedLyrics,
                        BlurAmount = (float)(
                            SettingsService.LyricsBlurAmount * (1 - i / (0.5 - step))
                        ),
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    };
                    using var topCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            sender.Size.Height * i,
                            sender.Size.Width,
                            sender.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    using var bottomCropped = new CropEffect
                    {
                        Source = blurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            sender.Size.Height * (1 - i - step * (1 + overlapFactor)),
                            sender.Size.Width,
                            sender.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    combinedBlurredLyricsDs.DrawImage(topCropped);
                    combinedBlurredLyricsDs.DrawImage(bottomCropped);
                }
            }

            // Masked mock gradient blurred lyrics layer
            using var maskedCombinedBlurredLyrics = new CanvasCommandList(sender);
            using (
                var maskedCombinedBlurredLyricsDs =
                    maskedCombinedBlurredLyrics.CreateDrawingSession()
            )
            {
                if (SettingsService.LyricsVerticalEdgeOpacity == 100)
                {
                    maskedCombinedBlurredLyricsDs.DrawImage(combinedBlurredLyrics);
                }
                else
                {
                    using var mask = new CanvasCommandList(sender);
                    using (var maskDs = mask.CreateDrawingSession())
                    {
                        DrawGradientOpacityMask(sender, maskDs, r, g, b);
                    }
                    maskedCombinedBlurredLyricsDs.DrawImage(
                        new AlphaMaskEffect { Source = combinedBlurredLyrics, AlphaMask = mask }
                    );
                }
            }

            // Draw the final composed layer
            ds.DrawImage(maskedCombinedBlurredLyrics);
        }

        private void DrawGradientOpacityMask(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            byte r,
            byte g,
            byte b
        )
        {
            byte verticalEdgeAlpha = (byte)(255 * SettingsService.LyricsVerticalEdgeOpacity / 100f);
            using var maskBrush = new CanvasLinearGradientBrush(
                control,
                [
                    new() { Position = 0, Color = Color.FromArgb(verticalEdgeAlpha, r, g, b) },
                    new() { Position = 0.5f, Color = Color.FromArgb(255, r, g, b) },
                    new() { Position = 1, Color = Color.FromArgb(verticalEdgeAlpha, r, g, b) },
                ]
            )
            {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height),
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        // Comsumes CPU related resources
        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _pureLyricsRenderer.CurrentTime += args.Timing.ElapsedTime;

            if (SettingsService.IsDynamicCoverOverlay)
            {
                _coverImageAsBackgroundRenderer.RotateAngle += _coverRotateSpeed;
                _coverImageAsBackgroundRenderer.RotateAngle %= MathF.PI * 2;
            }
            if (SettingsService.IsLyricsDynamicGlowEffectEnabled)
            {
                _lyricsGlowEffectAngle += _lyricsGlowEffectSpeed;
                _lyricsGlowEffectAngle %= MathF.PI * 2;
            }

            _coverImageAsBackgroundRenderer.Calculate(sender);

            if (_pureLyricsRenderer.LyricsLines.LastOrDefault()?.TextLayout == null)
            {
                _pureLyricsRenderer.ReLayoutAsync(sender);
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            _pureLyricsRenderer.CalculateScaleAndOpacity(currentPlayingLineIndex);
            _pureLyricsRenderer.CalculatePosition(sender, currentPlayingLineIndex);
        }

        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < _pureLyricsRenderer.LyricsLines.Count; i++)
            {
                var line = _pureLyricsRenderer.LyricsLines[i];
                if (line.EndPlayingTimestampMs < _pureLyricsRenderer.CurrentTime.TotalMilliseconds)
                {
                    continue;
                }
                return i;
            }

            return -1;
        }

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            InitMediaManager();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.SettingsWindow is null)
            {
                var settingsWindow = new BaseWindow();
                settingsWindow.Navigate(typeof(SettingsPage));
                App.Current.SettingsWindow = settingsWindow;
            }

            var appWindow = App.Current.SettingsWindow.AppWindow;

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }

            appWindow.Show();
            appWindow.MoveInZOrderAtTop();
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            SettingsService.IsFirstRun = false;
        }

        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }

        private void BottomCommandGrid_PointerEntered(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (SettingsService.IsImmersiveMode && BottomCommandGrid.Opacity == 0)
                BottomCommandGrid.Opacity = .5;
        }

        private void BottomCommandGrid_PointerExited(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (SettingsService.IsImmersiveMode && BottomCommandGrid.Opacity == .5)
                BottomCommandGrid.Opacity = 0;
        }

        private async void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _pureLyricsRenderer.LimitedLineWidth = e.NewSize.Width;
            await _pureLyricsRenderer.ReLayoutAsync(LyricsCanvas);
        }

        private async void LyricsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _pureLyricsRenderer.CanvasWidth = e.NewSize.Width;
            _pureLyricsRenderer.CanvasHeight = e.NewSize.Height;
            await _pureLyricsRenderer.ReLayoutAsync(LyricsCanvas);
        }
    }
}
