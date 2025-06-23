// 2025/6/23 by Zhe Fang

using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="LyricsRendererViewModel" />
    /// </summary>
    public partial class LyricsRendererViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<Color>>,
            IRecipient<PropertyChangedMessage<LyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<LyricsAlignmentType>>,
            IRecipient<PropertyChangedMessage<ElementTheme>>,
            IRecipient<PropertyChangedMessage<LyricsFontWeight>>,
            IRecipient<PropertyChangedMessage<LyricsGlowEffectScope>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>>>,
            IRecipient<PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>>>
    {
        #region Fields

        /// <summary>
        /// Defines the _albumArtBgTransition
        /// </summary>
        private readonly ValueTransition<float> _albumArtBgTransition = new(
            initialValue: 0f,
            durationSeconds: 1.0f,
            interpolator: (from, to, progress) => from + (to - from) * progress
        );

        /// <summary>
        /// Defines the _canvasYScrollTransition
        /// </summary>
        private readonly ValueTransition<float> _canvasYScrollTransition = new(
            initialValue: 0f,
            durationSeconds: 0.8f,
            interpolator: (from, to, progress) =>
                from + (to - from) * EasingHelper.SmootherStep(progress)
        );

        /// <summary>
        /// Defines the _coverRotateSpeed
        /// </summary>
        private readonly float _coverRotateSpeed = 0.003f;

        /// <summary>
        /// Defines the _defaultOpacity
        /// </summary>
        private readonly float _defaultOpacity = 0.3f;

        /// <summary>
        /// Defines the _defaultScale
        /// </summary>
        private readonly float _defaultScale = 0.95f;

        /// <summary>
        /// Defines the _highlightedOpacity
        /// </summary>
        private readonly float _highlightedOpacity = 1.0f;

        /// <summary>
        /// Defines the _highlightedScale
        /// </summary>
        private readonly float _highlightedScale = 1.0f;

        /// <summary>
        /// Defines the _immersiveBgrTransition
        /// </summary>
        private readonly ValueTransition<Color> _immersiveBgrTransition = new(
            initialValue: Colors.Transparent,
            durationSeconds: 0.3f,
            interpolator: (from, to, progress) =>
                Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
        );

        /// <summary>
        /// Defines the _libWatcherService
        /// </summary>
        private readonly ILibWatcherService _libWatcherService;

        /// <summary>
        /// Defines the _limitedLineWidthTransition
        /// </summary>
        private readonly ValueTransition<float> _limitedLineWidthTransition = new(
            initialValue: 0f,
            durationSeconds: 0.8f,
            interpolator: (from, to, progress) => to
        );

        /// <summary>
        /// Defines the _lineEnteringDurationMs
        /// </summary>
        private readonly int _lineEnteringDurationMs = 800;

        /// <summary>
        /// Defines the _lineExitingDurationMs
        /// </summary>
        private readonly int _lineExitingDurationMs = 800;

        /// <summary>
        /// Defines the _lyricsGlowEffectAmount
        /// </summary>
        private readonly float _lyricsGlowEffectAmount = 6f;

        /// <summary>
        /// Defines the _musicSearchService
        /// </summary>
        private protected readonly IMusicSearchService _musicSearchService;

        /// <summary>
        /// Defines the _playbackService
        /// </summary>
        private protected readonly IPlaybackService _playbackService;

        /// <summary>
        /// Defines the _rightMargin
        /// </summary>
        private readonly double _rightMargin = 36;

        /// <summary>
        /// Defines the _topMargin
        /// </summary>
        private readonly float _topMargin = 0f;

        /// <summary>
        /// Defines the _albumArtAccentColor
        /// </summary>
        private Color? _albumArtAccentColor = null;

        /// <summary>
        /// Defines the _albumArtBitmap
        /// </summary>
        private SoftwareBitmap? _albumArtBitmap = null;

        /// <summary>
        /// Defines the _darkFontColor
        /// </summary>
        private Color _darkFontColor = Colors.Black;

        /// <summary>
        /// Defines the _endVisibleLineIndex
        /// </summary>
        private int _endVisibleLineIndex = -1;

        /// <summary>
        /// Defines the _fontColor
        /// </summary>
        private protected Color _fontColor;

        /// <summary>
        /// Defines the _isPlaying
        /// </summary>
        private bool _isPlaying = true;

        /// <summary>
        /// Defines the _isRelayoutNeeded
        /// </summary>
        private protected bool _isRelayoutNeeded = true;

        /// <summary>
        /// Defines the _langIndex
        /// </summary>
        private int _langIndex = 0;

        /// <summary>
        /// Defines the _lastAlbumArtBitmap
        /// </summary>
        private SoftwareBitmap? _lastAlbumArtBitmap = null;

        /// <summary>
        /// Defines the _lightFontColor
        /// </summary>
        private Color _lightFontColor = Colors.White;

        /// <summary>
        /// Defines the _lyricsForGlowEffect
        /// </summary>
        private List<LyricsLine>? _lyricsForGlowEffect = [];

        /// <summary>
        /// Defines the _multiLangLyrics
        /// </summary>
        private List<List<LyricsLine>> _multiLangLyrics = [];

        /// <summary>
        /// Defines the _rotateAngle
        /// </summary>
        private float _rotateAngle = 0f;

        /// <summary>
        /// Defines the _startVisibleLineIndex
        /// </summary>
        private int _startVisibleLineIndex = -1;

        /// <summary>
        /// Defines the _textFormat
        /// </summary>
        private protected CanvasTextFormat _textFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsRendererViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        /// <param name="playbackService">The playbackService<see cref="IPlaybackService"/></param>
        /// <param name="musicSearchService">The musicSearchService<see cref="IMusicSearchService"/></param>
        /// <param name="libWatcherService">The libWatcherService<see cref="ILibWatcherService"/></param>
        public LyricsRendererViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService,
            IMusicSearchService musicSearchService,
            ILibWatcherService libWatcherService
        )
            : base(settingsService)
        {
            _musicSearchService = musicSearchService;
            _playbackService = playbackService;
            _libWatcherService = libWatcherService;

            CoverImageRadius = _settingsService.CoverImageRadius;
            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            LyricsFontColorType = _settingsService.LyricsFontColorType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.PositionChanged += PlaybackService_PositionChanged;

            RefreshPlaybackInfo();
            UpdateFontColor();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the CoverImageRadius
        /// </summary>
        public int CoverImageRadius { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayBlurAmount
        /// </summary>
        public int CoverOverlayBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the CoverOverlayOpacity
        /// </summary>
        public int CoverOverlayOpacity { get; set; }

        /// <summary>
        /// Gets or sets the DisplayType
        /// </summary>
        public LyricsDisplayType DisplayType { get; set; }

        /// <summary>
        /// Gets or sets the ElapsedTime
        /// </summary>
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether IsCoverOverlayEnabled
        /// </summary>
        public bool IsCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsDynamicCoverOverlayEnabled
        /// </summary>
        public bool IsDynamicCoverOverlayEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsLyricsGlowEffectEnabled
        /// </summary>
        public bool IsLyricsGlowEffectEnabled { get; set; }

        /// <summary>
        /// Gets or sets the LyricsAlignmentType
        /// </summary>
        public LyricsAlignmentType LyricsAlignmentType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsBlurAmount
        /// </summary>
        public int LyricsBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontColorType
        /// </summary>
        [ObservableProperty]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontWeight
        /// </summary>
        [ObservableProperty]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        /// <summary>
        /// Gets or sets the LyricsGlowEffectScope
        /// </summary>
        public LyricsGlowEffectScope LyricsGlowEffectScope { get; set; }

        /// <summary>
        /// Gets or sets the LyricsLineSpacingFactor
        /// </summary>
        [ObservableProperty]
        public partial float LyricsLineSpacingFactor { get; set; }

        /// <summary>
        /// Gets or sets the LyricsStatus
        /// </summary>
        [NotifyPropertyChangedRecipients]
        [ObservableProperty]
        public partial LyricsStatus LyricsStatus { get; set; } = LyricsStatus.Loading;

        /// <summary>
        /// Gets or sets the LyricsVerticalEdgeOpacity
        /// </summary>
        public int LyricsVerticalEdgeOpacity { get; set; }

        /// <summary>
        /// Gets or sets the SongInfo
        /// </summary>
        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        /// <summary>
        /// Gets or sets the Theme
        /// </summary>
        [ObservableProperty]
        public partial ElementTheme Theme { get; set; }

        /// <summary>
        /// Gets or sets the TotalTime
        /// </summary>
        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets a value indicating whether IsDockMode
        /// </summary>
        private bool IsDockMode { get; set; } = false;

        #endregion

        #region Methods

        /// <summary>
        /// The Draw
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (IsCoverOverlayEnabled)
            {
                DrawAlbumArtBackground(control, ds);
            }

            if (IsDockMode)
            {
                DrawImmersiveBackground(control, ds, IsCoverOverlayEnabled);
            }

            // Original lyrics only layer
            using var lyrics = new CanvasCommandList(control);
            using (var lyricsDs = lyrics.CreateDrawingSession())
            {
                switch (DisplayType)
                {
                    case LyricsDisplayType.AlbumArtOnly:
                    case LyricsDisplayType.PlaceholderOnly:
                        break;
                    case LyricsDisplayType.LyricsOnly:
                    case LyricsDisplayType.SplitView:
                        DrawLyrics(
                            control,
                            lyricsDs,
                            _multiLangLyrics.SafeGet(_langIndex),
                            _defaultOpacity,
                            LyricsHighlightType.LineByLine
                        );
                        break;
                    default:
                        break;
                }
            }

            // Lyrics layer with opacity modification (used for glow effect)
            using var modifiedLyrics = new CanvasCommandList(control);
            using (var modifiedLyricsDs = modifiedLyrics.CreateDrawingSession())
            {
                if (IsLyricsGlowEffectEnabled)
                {
                    switch (DisplayType)
                    {
                        case LyricsDisplayType.AlbumArtOnly:
                        case LyricsDisplayType.PlaceholderOnly:
                            break;
                        case LyricsDisplayType.LyricsOnly:
                        case LyricsDisplayType.SplitView:
                            switch (LyricsGlowEffectScope)
                            {
                                case LyricsGlowEffectScope.WholeLyrics:
                                    modifiedLyricsDs.DrawImage(lyrics);
                                    break;
                                case LyricsGlowEffectScope.CurrentLine:
                                    DrawLyrics(
                                        control,
                                        modifiedLyricsDs,
                                        _lyricsForGlowEffect,
                                        0,
                                        LyricsHighlightType.LineByLine
                                    );
                                    break;
                                case LyricsGlowEffectScope.CurrentChar:
                                    DrawLyrics(
                                        control,
                                        modifiedLyricsDs,
                                        _lyricsForGlowEffect,
                                        0,
                                        LyricsHighlightType.CharByChar
                                    );
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            using var glowedLyrics = new CanvasCommandList(control);
            using (var glowedLyricsDs = glowedLyrics.CreateDrawingSession())
            {
                glowedLyricsDs.DrawImage(
                    new ShadowEffect
                    {
                        Source = modifiedLyrics,
                        BlurAmount = _lyricsGlowEffectAmount,
                        ShadowColor = _fontColor,
                        Optimization = EffectOptimization.Quality,
                    }
                );
                glowedLyricsDs.DrawImage(lyrics);
            }

            // Mock gradient blurred lyrics layer
            using var blurredLyrics = new CanvasCommandList(control);
            using var blurredLyricsDs = blurredLyrics.CreateDrawingSession();
            if (LyricsBlurAmount == 0)
            {
                blurredLyricsDs.DrawImage(glowedLyrics);
            }
            else
            {
                double step = 0.05;
                double overlapFactor = 0;
                for (double i = 0; i <= 0.5 - step; i += step)
                {
                    using var halfBlurredLyrics = new GaussianBlurEffect
                    {
                        Source = glowedLyrics,
                        BlurAmount = (float)(LyricsBlurAmount * (1 - i / (0.5 - step))),
                        Optimization = EffectOptimization.Quality,
                        BorderMode = EffectBorderMode.Soft,
                    };
                    using var topCropped = new CropEffect
                    {
                        Source = halfBlurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            control.Size.Height * i,
                            control.Size.Width,
                            control.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    using var bottomCropped = new CropEffect
                    {
                        Source = halfBlurredLyrics,
                        SourceRectangle = new Rect(
                            0,
                            control.Size.Height * (1 - i - step * (1 + overlapFactor)),
                            control.Size.Width,
                            control.Size.Height * step * (1 + overlapFactor)
                        ),
                    };
                    blurredLyricsDs.DrawImage(topCropped);
                    blurredLyricsDs.DrawImage(bottomCropped);
                }
            }

            // Masked mock gradient blurred lyrics layer
            using var maskedBlurredLyrics = new CanvasCommandList(control);
            using (var maskedBlurredLyricsDs = maskedBlurredLyrics.CreateDrawingSession())
            {
                if (LyricsVerticalEdgeOpacity == 100)
                {
                    maskedBlurredLyricsDs.DrawImage(blurredLyrics);
                }
                else
                {
                    using var mask = new CanvasCommandList(control);
                    using (var maskDs = mask.CreateDrawingSession())
                    {
                        DrawGradientOpacityMask(control, maskDs);
                    }
                    maskedBlurredLyricsDs.DrawImage(
                        new AlphaMaskEffect { Source = blurredLyrics, AlphaMask = mask }
                    );
                }
            }

            // Draw the final composed layer
            ds.DrawImage(maskedBlurredLyrics);
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ObservableCollection{LyricsSearchProviderInfo}}"/></param>
        public void Receive(
            PropertyChangedMessage<ObservableCollection<LyricsSearchProviderInfo>> message
        )
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.LyricsSearchProvidersInfo))
                {
                    // Lyrics search providers info changed, re-fetch lyrics
                    RefreshLyricsAsync().ConfigureAwait(true);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{bool}"/></param>
        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.IsDynamicCoverOverlayEnabled))
                {
                    IsDynamicCoverOverlayEnabled = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.IsCoverOverlayEnabled))
                {
                    IsCoverOverlayEnabled = message.NewValue;
                }
            }
            else if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.IsLyricsGlowEffectEnabled)
                )
                {
                    IsLyricsGlowEffectEnabled = message.NewValue;
                }
            }
            else if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.IsDockMode))
                {
                    IsDockMode = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{Color}"/></param>
        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.ActivatedWindowAccentColor))
                {
                    _immersiveBgrTransition.StartTransition(message.NewValue);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{double}"/></param>
        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.LimitedLineWidth))
                {
                    _limitedLineWidthTransition.StartTransition((float)message.NewValue);
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ElementTheme}"/></param>
        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.ThemeType))
                {
                    Theme = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{float}"/></param>
        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsLineSpacingFactor)
                )
                {
                    LyricsLineSpacingFactor = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{int}"/></param>
        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.CoverImageRadius))
                {
                    CoverImageRadius = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.CoverOverlayOpacity))
                {
                    CoverOverlayOpacity = message.NewValue;
                }
                else if (message.PropertyName == nameof(SettingsViewModel.CoverOverlayBlurAmount))
                {
                    CoverOverlayBlurAmount = message.NewValue;
                }
            }
            else if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsVerticalEdgeOpacity)
                )
                {
                    LyricsVerticalEdgeOpacity = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsBlurAmount)
                )
                {
                    LyricsBlurAmount = message.NewValue;
                }
                else if (
                    message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontSize)
                )
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsAlignmentType}"/></param>
        public void Receive(PropertyChangedMessage<LyricsAlignmentType> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsAlignmentType)
                )
                {
                    LyricsAlignmentType = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsDisplayType}"/></param>
        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            DisplayType = message.NewValue;
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsFontColorType}"/></param>
        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsFontColorType)
                )
                {
                    LyricsFontColorType = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsFontWeight}"/></param>
        public void Receive(PropertyChangedMessage<LyricsFontWeight> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsFontWeight))
                {
                    LyricsFontWeight = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{LyricsGlowEffectScope}"/></param>
        public void Receive(PropertyChangedMessage<LyricsGlowEffectScope> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSettingsControlViewModel.LyricsGlowEffectScope))
                {
                    LyricsGlowEffectScope = message.NewValue;
                }
            }
        }

        /// <summary>
        /// The Receive
        /// </summary>
        /// <param name="message">The message<see cref="PropertyChangedMessage{ObservableCollection{LocalLyricsFolder}}"/></param>
        public void Receive(PropertyChangedMessage<ObservableCollection<LocalLyricsFolder>> message)
        {
            if (message.Sender is SettingsViewModel)
            {
                if (message.PropertyName == nameof(SettingsViewModel.LocalLyricsFolders))
                {
                    // Music lib changed, re-fetch lyrics
                    RefreshLyricsAsync().ConfigureAwait(true);
                }
            }
        }

        /// <summary>
        /// The RefreshPlaybackInfo
        /// </summary>
        public void RefreshPlaybackInfo()
        {
            _isPlaying = _playbackService.IsPlaying;
            SongInfo = _playbackService.SongInfo;
            TotalTime = _playbackService.Position;
        }

        /// <summary>
        /// The RequestRelayout
        /// </summary>
        public void RequestRelayout()
        {
            _isRelayoutNeeded = true;
        }

        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="args">The args<see cref="CanvasAnimatedUpdateEventArgs"/></param>
        public void Update(ICanvasAnimatedControl control, CanvasAnimatedUpdateEventArgs args)
        {
            if (_isPlaying)
            {
                TotalTime += args.Timing.ElapsedTime;
            }

            ElapsedTime = args.Timing.ElapsedTime;

            if (_immersiveBgrTransition.IsTransitioning)
            {
                _immersiveBgrTransition.Update(ElapsedTime);
            }

            if (_albumArtBgTransition.IsTransitioning)
            {
                _albumArtBgTransition.Update(ElapsedTime);
            }

            if (IsDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            if (_limitedLineWidthTransition.IsTransitioning)
            {
                _limitedLineWidthTransition.Update(ElapsedTime);
                _isRelayoutNeeded = true;
            }

            if (_isRelayoutNeeded)
            {
                ReLayout(control);
                _isRelayoutNeeded = false;
            }

            UpdateLinesProps(_multiLangLyrics.SafeGet(_langIndex), _defaultOpacity);
            UpdateCanvasYScrollOffset(control);

            if (IsLyricsGlowEffectEnabled)
            {
                // Deep copy lyrics lines for glow effect
                _lyricsForGlowEffect = _multiLangLyrics
                    .SafeGet(_langIndex)
                    ?.Select(line => line.Clone())
                    .ToList();
                switch (LyricsGlowEffectScope)
                {
                    case LyricsGlowEffectScope.WholeLyrics:
                        break;
                    case LyricsGlowEffectScope.CurrentLine:
                        UpdateLinesProps(_lyricsForGlowEffect, 0);
                        break;
                    case LyricsGlowEffectScope.CurrentChar:
                        UpdateLinesProps(_lyricsForGlowEffect, 0);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// The DrawImgae
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="softwareBitmap">The softwareBitmap<see cref="SoftwareBitmap"/></param>
        /// <param name="opacity">The opacity<see cref="float"/></param>
        private static void DrawImgae(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            SoftwareBitmap softwareBitmap,
            float opacity
        )
        {
            using var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(control, softwareBitmap);
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            var scaleFactor =
                (float)Math.Sqrt(Math.Pow(control.Size.Width, 2) + Math.Pow(control.Size.Height, 2))
                / Math.Min(imageWidth, imageHeight);

            ds.DrawImage(
                new OpacityEffect
                {
                    Source = new ScaleEffect
                    {
                        InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
                        BorderMode = EffectBorderMode.Hard,
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    Opacity = opacity,
                },
                (float)control.Size.Width / 2 - imageWidth * scaleFactor / 2,
                (float)control.Size.Height / 2 - imageHeight * scaleFactor / 2
            );
        }

        /// <summary>
        /// The DrawAlbumArtBackground
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            ds.Transform = Matrix3x2.CreateRotation(_rotateAngle, control.Size.ToVector2() * 0.5f);

            var overlappedCovers = new CanvasCommandList(control.Device);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_albumArtBgTransition.IsTransitioning)
            {
                if (_lastAlbumArtBitmap != null)
                {
                    DrawImgae(
                        control,
                        overlappedCoversDs,
                        _lastAlbumArtBitmap,
                        1 - _albumArtBgTransition.Value
                    );
                }
                if (_albumArtBitmap != null)
                {
                    DrawImgae(
                        control,
                        overlappedCoversDs,
                        _albumArtBitmap,
                        _albumArtBgTransition.Value
                    );
                }
            }
            else if (_albumArtBitmap != null)
            {
                DrawImgae(control, overlappedCoversDs, _albumArtBitmap, 1f);
            }

            using var coverOverlayEffect = new OpacityEffect
            {
                Opacity = CoverOverlayOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = CoverOverlayBlurAmount,
                    Source = overlappedCovers,
                },
            };
            ds.DrawImage(coverOverlayEffect);

            ds.Transform = Matrix3x2.Identity;
        }

        /// <summary>
        /// The DrawGradientOpacityMask
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        private void DrawGradientOpacityMask(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds
        )
        {
            byte verticalEdgeAlpha = (byte)(255 * LyricsVerticalEdgeOpacity / 100f);
            using var maskBrush = new CanvasLinearGradientBrush(
                control,
                [
                    new() { Position = 0, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                    new() { Position = 0.5f, Color = Color.FromArgb(255, 0, 0, 0) },
                    new() { Position = 1, Color = Color.FromArgb(verticalEdgeAlpha, 0, 0, 0) },
                ]
            )
            {
                StartPoint = new Vector2(0, 0),
                EndPoint = new Vector2(0, (float)control.Size.Height),
            };
            ds.FillRectangle(new Rect(0, 0, control.Size.Width, control.Size.Height), maskBrush);
        }

        /// <summary>
        /// The DrawImmersiveBackground
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="withGradient">The withGradient<see cref="bool"/></param>
        private void DrawImmersiveBackground(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            bool withGradient
        )
        {
            ds.FillRectangle(
                new Rect(0, 0, control.Size.Width, control.Size.Height),
                new CanvasLinearGradientBrush(
                    control,
                    [
                        new CanvasGradientStop
                        {
                            Position = 0f,
                            Color = withGradient
                                ? Color.FromArgb(
                                    211,
                                    _immersiveBgrTransition.Value.R,
                                    _immersiveBgrTransition.Value.G,
                                    _immersiveBgrTransition.Value.B
                                )
                                : _immersiveBgrTransition.Value,
                        },
                        new CanvasGradientStop
                        {
                            Position = 1,
                            Color = _immersiveBgrTransition.Value,
                        },
                    ]
                )
                {
                    StartPoint = new Vector2(0, 0),
                    EndPoint = new Vector2(0, (float)control.Size.Height),
                }
            );
        }

        /// <summary>
        /// The DrawLyrics
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="ds">The ds<see cref="CanvasDrawingSession"/></param>
        /// <param name="source">The source<see cref="List{LyricsLine}?"/></param>
        /// <param name="defaultOpacity">The defaultOpacity<see cref="float"/></param>
        /// <param name="currentLineHighlightType">The currentLineHighlightType<see cref="LyricsHighlightType"/></param>
        private void DrawLyrics(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            List<LyricsLine>? source,
            float defaultOpacity,
            LyricsHighlightType currentLineHighlightType
        )
        {
            var (displayStartLineIndex, displayEndLineIndex) =
                GetVisibleLyricsLineIndexBoundaries();

            for (
                int i = displayStartLineIndex;
                source?.Count > 0 && i >= 0 && i < source?.Count && i <= displayEndLineIndex;
                i++
            )
            {
                var line = source?[i];

                using var textLayout = new CanvasTextLayout(
                    control,
                    line?.Text,
                    _textFormat,
                    (float)_limitedLineWidthTransition.Value,
                    (float)control.Size.Height
                );

                float progressPerChar = 1f / line.Text.Length;

                var position = new Vector2(line.Position.X, line.Position.Y);

                float centerX = position.X;
                float centerY = position.Y + (float)textLayout.LayoutBounds.Height / 2;

                switch (LyricsAlignmentType)
                {
                    case LyricsAlignmentType.Left:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                        break;
                    case LyricsAlignmentType.Center:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                        centerX += (float)_limitedLineWidthTransition.Value / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)_limitedLineWidthTransition.Value;
                        break;
                    default:
                        break;
                }

                int startIndex = 0;

                // Set brush
                for (int j = 0; j < textLayout.LineCount; j++)
                {
                    int count = textLayout.LineMetrics[j].CharacterCount;
                    var regions = textLayout.GetCharacterRegions(startIndex, count);
                    float subLinePlayingProgress = Math.Clamp(
                        (line.PlayingProgress * line.Text.Length - startIndex) / count,
                        0,
                        1
                    );

                    float startX = (float)(regions[0].LayoutBounds.Left + position.X);
                    float endX = (float)(regions[^1].LayoutBounds.Right + position.X);

                    if (currentLineHighlightType == LyricsHighlightType.LineByLine)
                    {
                        float[] pos =
                        [
                            0,
                            subLinePlayingProgress * (1 + progressPerChar) - progressPerChar,
                            subLinePlayingProgress * (1 + progressPerChar),
                            1.5f,
                        ];
                        float[] opacity =
                        [
                            line.Opacity,
                            line.Opacity,
                            defaultOpacity,
                            defaultOpacity,
                        ];

                        using var brush = GetHorizontalFillBrush(
                            control,
                            pos,
                            opacity,
                            startX,
                            endX
                        );
                        textLayout.SetBrush(startIndex, count, brush);
                    }
                    else if (currentLineHighlightType == LyricsHighlightType.CharByChar)
                    {
                        float[] pos =
                        [
                            subLinePlayingProgress * (1 + progressPerChar) - 3 * progressPerChar,
                            subLinePlayingProgress * (1 + progressPerChar) - progressPerChar,
                            subLinePlayingProgress * (1 + progressPerChar),
                            1.5f,
                        ];
                        float[] opacity =
                        [
                            defaultOpacity,
                            line.Opacity,
                            defaultOpacity,
                            defaultOpacity,
                        ];

                        using var brush = GetHorizontalFillBrush(
                            control,
                            pos,
                            opacity,
                            startX,
                            endX
                        );
                        textLayout.SetBrush(startIndex, count, brush);
                    }

                    startIndex += count;
                }

                // Scale
                ds.Transform =
                    Matrix3x2.CreateScale(line.Scale, new Vector2(centerX, centerY))
                    * Matrix3x2.CreateTranslation(
                        (float)(
                            control.Size.Width - _rightMargin - _limitedLineWidthTransition.Value
                        ),
                        _canvasYScrollTransition.Value + (float)(control.Size.Height / 2)
                    );

                ds.DrawTextLayout(textLayout, position, Colors.Transparent);
                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        /// <summary>
        /// The GetCurrentPlayingLineIndex
        /// </summary>
        /// <returns>The <see cref="int"/></returns>
        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < _multiLangLyrics.SafeGet(_langIndex)?.Count; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?[i];
                if (line?.EndMs < TotalTime.TotalMilliseconds)
                {
                    continue;
                }
                return i;
            }

            return -1;
        }

        /// <summary>
        /// The GetHorizontalFillBrush
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        /// <param name="stopPosition">The stopPosition<see cref="float[]"/></param>
        /// <param name="stopOpacity">The stopOpacity<see cref="float[]"/></param>
        /// <param name="startX">The startX<see cref="float"/></param>
        /// <param name="endX">The endX<see cref="float"/></param>
        /// <returns>The <see cref="CanvasLinearGradientBrush"/></returns>
        private CanvasLinearGradientBrush GetHorizontalFillBrush(
            ICanvasAnimatedControl control,
            float[] stopPosition,
            float[] stopOpacity,
            float startX,
            float endX
        )
        {
            var r = _fontColor.R;
            var g = _fontColor.G;
            var b = _fontColor.B;

            return new CanvasLinearGradientBrush(
                control,
                [
                    new()
                    {
                        Position = stopPosition[0],
                        Color = Color.FromArgb((byte)(255 * stopOpacity[0]), r, g, b),
                    },
                    new()
                    {
                        Position = stopPosition[1],
                        Color = Color.FromArgb((byte)(255 * stopOpacity[1]), r, g, b),
                    },
                    new()
                    {
                        Position = stopPosition[2],
                        Color = Color.FromArgb((byte)(255 * stopOpacity[2]), r, g, b),
                    },
                    new()
                    {
                        Position = stopPosition[3],
                        Color = Color.FromArgb((byte)(255 * stopOpacity[3]), r, g, b),
                    },
                ]
            )
            {
                StartPoint = new Vector2(startX, 0),
                EndPoint = new Vector2(endX, 0),
            };
        }

        /// <summary>
        /// The GetLinePlayingProgress
        /// </summary>
        /// <param name="line">The line<see cref="LyricsLine"/></param>
        /// <returns>The <see cref="float"/></returns>
        private float GetLinePlayingProgress(LyricsLine line)
        {
            float playProgress = 0f;
            int now = (int)TotalTime.TotalMilliseconds;

            if (line.CharTimings != null && line.CharTimings.Count > 0)
            {
                int charIndex = 0;
                for (; charIndex < line.CharTimings.Count; charIndex++)
                {
                    var timing = line.CharTimings[charIndex];
                    if (now < timing.StartMs)
                    {
                        // 当前时间还没到这个字，停在上一个字
                        break;
                    }
                    if (now >= timing.StartMs && now <= timing.EndMs)
                    {
                        float charProgress = 1f;
                        if (timing.EndMs != timing.StartMs)
                        {
                            charProgress =
                                (now - timing.StartMs) / (float)(timing.EndMs - timing.StartMs);
                        }
                        // 当前时间在这个字的高亮区间
                        playProgress = charIndex + charProgress;
                        playProgress /= line.CharTimings.Count;
                        return playProgress;
                    }
                }
                // 如果超出最后一个字的结束时间
                if (now > line.CharTimings[^1].EndMs)
                {
                    // 如果还没到行尾，保持最后一个字高亮
                    if (now < line.EndMs)
                    {
                        playProgress = 1f; // 全部字高亮
                    }
                    else
                    {
                        playProgress = 1f; // 行已结束
                    }
                }
                else if (charIndex == 0)
                {
                    playProgress = 0f; // 还没到第一个字
                }
            }
            else
            {
                playProgress = (now - line.StartMs) / (float)(line.DurationMs);
            }
            return playProgress;
        }

        /// <summary>
        /// The GetMaxLyricsLineIndexBoundaries
        /// </summary>
        /// <returns>The <see cref="Tuple{int, int}"/></returns>
        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (
                SongInfo == null
                || _multiLangLyrics.SafeGet(_langIndex) == null
                || _multiLangLyrics[_langIndex].Count == 0
            )
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _multiLangLyrics[_langIndex].Count - 1);
        }

        /// <summary>
        /// The GetVisibleLyricsLineIndexBoundaries
        /// </summary>
        /// <returns>The <see cref="Tuple{int, int}"/></returns>
        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries()
        {
            // _logger.LogDebug($"{_startVisibleLineIndex} {_endVisibleLineIndex}");
            return new Tuple<int, int>(_startVisibleLineIndex, _endVisibleLineIndex);
        }

        /// <summary>
        /// The LibWatcherService_MusicLibraryFilesChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="Events.LibChangedEventArgs"/></param>
        private void LibWatcherService_MusicLibraryFilesChanged(
            object? sender,
            Events.LibChangedEventArgs e
        )
        {
            RefreshLyricsAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// The PlaybackService_IsPlayingChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="IsPlayingChangedEventArgs"/></param>
        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            _isPlaying = e.IsPlaying;
        }

        /// <summary>
        /// The PlaybackService_PositionChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="PositionChangedEventArgs"/></param>
        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            TotalTime = e.Position;
        }

        /// <summary>
        /// The PlaybackService_SongInfoChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="SongInfoChangedEventArgs"/></param>
        private void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
        }

        /// <summary>
        /// Should invoke this function when:
        /// 1. The song info is changed (new song is played).
        /// 2. Lyrics search provider info is changed (change order, enable or disable any provider).
        /// 3. Local music/lyrics files are changed (added, removed, renamed)
        /// </summary>
        /// <returns></returns>
        private async Task RefreshLyricsAsync()
        {
            _multiLangLyrics = [];
            _isRelayoutNeeded = true;
            LyricsStatus = LyricsStatus.Loading;
            string? lyricsRaw = null;
            LyricsFormat? lyricsFormat = null;

            if (SongInfo != null)
            {
                (lyricsRaw, lyricsFormat) = await _musicSearchService.SearchLyricsAsync(
                    SongInfo.Title,
                    SongInfo.Artist,
                    SongInfo.Album ?? "",
                    SongInfo.DurationMs ?? 0
                );
            }

            if (lyricsRaw == null)
            {
                LyricsStatus = LyricsStatus.NotFound;
            }
            else if (SongInfo != null)
            {
                _multiLangLyrics = new LyricsParser().Parse(
                    lyricsRaw,
                    lyricsFormat,
                    SongInfo.Title,
                    SongInfo.Artist,
                    (int)(SongInfo.DurationMs ?? 0)
                );
                _isRelayoutNeeded = true;
                LyricsStatus = LyricsStatus.Found;
            }
        }

        /// <summary>
        /// Reassigns positions (x,y) to lyrics lines based on the current control size and font size
        /// </summary>
        /// <param name="control"></param>
        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            _textFormat.FontSize = LyricsFontSize;

            float y = _topMargin;

            // Init Positions
            for (int i = 0; i < _multiLangLyrics.SafeGet(_langIndex)?.Count; i++)
            {
                var line = _multiLangLyrics[_langIndex].SafeGet(i);

                if (line == null)
                {
                    continue;
                }

                // Calculate layout bounds
                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)_limitedLineWidthTransition.Value,
                    (float)control.Size.Height
                );
                line.Position = new Vector2(0, y);

                y +=
                    (float)textLayout.LayoutBounds.Height
                    / textLayout.LineCount
                    * (textLayout.LineCount + LyricsLineSpacingFactor);
            }
        }

        /// <summary>
        /// The UpdateCanvasYScrollOffset
        /// </summary>
        /// <param name="control">The control<see cref="ICanvasAnimatedControl"/></param>
        private void UpdateCanvasYScrollOffset(ICanvasAnimatedControl control)
        {
            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();
            if (currentPlayingLineIndex < 0)
            {
                return;
            }

            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            if (startLineIndex < 0 || endLineIndex < 0)
            {
                return;
            }

            // Set _scrollOffsetY
            LyricsLine? currentPlayingLine = _multiLangLyrics
                .SafeGet(_langIndex)
                ?[currentPlayingLineIndex];

            if (currentPlayingLine == null)
            {
                return;
            }

            using var playingTextLayout = new CanvasTextLayout(
                control,
                currentPlayingLine.Text,
                _textFormat,
                (float)_limitedLineWidthTransition.Value,
                (float)control.Size.Height
            );

            float targetYScrollOffset =
                (float?)(
                    -currentPlayingLine.Position.Y
                    + _multiLangLyrics.SafeGet(_langIndex)?[0].Position.Y
                    - playingTextLayout.LayoutBounds.Height / 2
                ) ?? 0f;

            if (!_canvasYScrollTransition.IsTransitioning)
            {
                _canvasYScrollTransition.StartTransition(targetYScrollOffset);
            }

            if (_canvasYScrollTransition.IsTransitioning)
            {
                _canvasYScrollTransition.Update(ElapsedTime);
            }

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update visible line indices
            for (int i = startLineIndex; i <= endLineIndex; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?.SafeGet(i);

                if (line == null)
                {
                    continue;
                }

                using var textLayout = new CanvasTextLayout(
                    control,
                    line?.Text,
                    _textFormat,
                    (float)_limitedLineWidthTransition.Value,
                    (float)control.Size.Height
                );

                if (
                    _canvasYScrollTransition.Value
                        + (float)(control.Size.Height / 2)
                        + line.Position.Y
                        + textLayout.LayoutBounds.Height
                    >= 0
                )
                {
                    if (_startVisibleLineIndex == -1)
                    {
                        _startVisibleLineIndex = i;
                    }
                }
                if (
                    _canvasYScrollTransition.Value
                        + (float)(control.Size.Height / 2)
                        + line.Position.Y
                        + textLayout.LayoutBounds.Height
                    >= control.Size.Height
                )
                {
                    if (_endVisibleLineIndex == -1)
                    {
                        _endVisibleLineIndex = i;
                    }
                }
            }

            if (_startVisibleLineIndex != -1 && _endVisibleLineIndex == -1)
            {
                _endVisibleLineIndex = endLineIndex;
            }
        }

        /// <summary>
        /// The UpdateFontColor
        /// </summary>
        private protected void UpdateFontColor()
        {
            Color fallback = Colors.Transparent;
            switch (Theme)
            {
                case ElementTheme.Default:
                    switch (Application.Current.RequestedTheme)
                    {
                        case ApplicationTheme.Light:
                            fallback = _darkFontColor;
                            break;
                        case ApplicationTheme.Dark:
                            fallback = _lightFontColor;
                            break;
                        default:
                            break;
                    }
                    break;
                case ElementTheme.Light:
                    fallback = _darkFontColor;
                    break;
                case ElementTheme.Dark:
                    fallback = _lightFontColor;
                    break;
                default:
                    break;
            }

            switch (LyricsFontColorType)
            {
                case LyricsFontColorType.Default:
                    _fontColor = fallback;
                    break;
                case LyricsFontColorType.Dominant:
                    _fontColor = _albumArtAccentColor ?? fallback;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// The UpdateLinesProps
        /// </summary>
        /// <param name="source">The source<see cref="List{LyricsLine}?"/></param>
        /// <param name="defaultOpacity">The defaultOpacity<see cref="float"/></param>
        private void UpdateLinesProps(List<LyricsLine>? source, float defaultOpacity)
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            for (int i = startLineIndex; i <= endLineIndex; i++)
            {
                var line = source?.SafeGet(i);

                if (line == null)
                {
                    continue;
                }

                bool linePlaying = i == currentPlayingLineIndex;

                var lineEnteringDurationMs = Math.Min(line.DurationMs, _lineEnteringDurationMs);
                var lineExitingDurationMs = _lineExitingDurationMs;
                if (i + 1 <= endLineIndex)
                {
                    lineExitingDurationMs = Math.Min(
                        source?[i + 1].DurationMs ?? 0,
                        lineExitingDurationMs
                    );
                }

                float lineEnteringProgress = 0.0f;
                float lineExitingProgress = 0.0f;

                bool lineEntering = false;
                bool lineExiting = false;

                float scale = _defaultScale;
                float opacity = defaultOpacity;

                float playProgress = 0;

                if (linePlaying)
                {
                    line.PlayingState = LyricsPlayingState.Playing;

                    scale = _highlightedScale;
                    opacity = _highlightedOpacity;

                    playProgress = GetLinePlayingProgress(line);

                    var durationFromStartMs = TotalTime.TotalMilliseconds - line.StartMs;
                    lineEntering = durationFromStartMs <= lineEnteringDurationMs;
                    if (lineEntering)
                    {
                        lineEnteringProgress = (float)durationFromStartMs / lineEnteringDurationMs;
                        scale =
                            _defaultScale
                            + (_highlightedScale - _defaultScale) * (float)lineEnteringProgress;
                        opacity =
                            defaultOpacity
                            + (_highlightedOpacity - defaultOpacity) * (float)lineEnteringProgress;
                    }
                }
                else
                {
                    if (i < currentPlayingLineIndex)
                    {
                        line.PlayingState = LyricsPlayingState.Played;
                        playProgress = 1;

                        var durationToEndMs = TotalTime.TotalMilliseconds - line.EndMs;
                        lineExiting = durationToEndMs <= lineExitingDurationMs;
                        if (lineExiting)
                        {
                            lineExitingProgress = (float)durationToEndMs / lineExitingDurationMs;
                            scale =
                                _highlightedScale
                                - (_highlightedScale - _defaultScale) * (float)lineExitingProgress;
                            opacity =
                                _highlightedOpacity
                                - (_highlightedOpacity - defaultOpacity)
                                    * (float)lineExitingProgress;
                        }
                    }
                    else
                    {
                        line.PlayingState = LyricsPlayingState.NotPlayed;
                    }
                }

                line.EnteringProgress = lineEnteringProgress;
                line.ExitingProgress = lineExitingProgress;

                line.Scale = scale;
                line.Opacity = opacity;

                line.PlayingProgress = playProgress;
            }
        }

        /// <summary>
        /// The OnLyricsFontColorTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontColorType"/></param>
        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            UpdateFontColor();
        }

        /// <summary>
        /// The OnLyricsFontSizeChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnLyricsFontSizeChanged(int value)
        {
            _isRelayoutNeeded = true;
        }

        /// <summary>
        /// The OnLyricsFontWeightChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontWeight"/></param>
        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _textFormat.FontWeight = value.ToFontWeight();
        }

        /// <summary>
        /// The OnLyricsLineSpacingFactorChanged
        /// </summary>
        /// <param name="value">The value<see cref="float"/></param>
        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isRelayoutNeeded = true;
        }

        /// <summary>
        /// The OnSongInfoChanged
        /// </summary>
        /// <param name="oldValue">The oldValue<see cref="SongInfo?"/></param>
        /// <param name="newValue">The newValue<see cref="SongInfo?"/></param>
        async partial void OnSongInfoChanged(SongInfo? oldValue, SongInfo? newValue)
        {
            TotalTime = TimeSpan.Zero;

            _lastAlbumArtBitmap = _albumArtBitmap;

            if (newValue?.AlbumArt is byte[] bytes)
            {
                _albumArtBitmap = await (
                    await ImageHelper.GetDecoderFromByte(bytes)
                ).GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                _albumArtAccentColor = (
                    await ImageHelper.GetAccentColorsFromByte(bytes)
                ).FirstOrDefault();
            }
            else
            {
                _albumArtBitmap = null;
                _albumArtAccentColor = null;
            }

            UpdateFontColor();

            _albumArtBgTransition.Reset(0f);
            _albumArtBgTransition.StartTransition(1f);

            await RefreshLyricsAsync();
        }

        /// <summary>
        /// The OnThemeChanged
        /// </summary>
        /// <param name="value">The value<see cref="ElementTheme"/></param>
        partial void OnThemeChanged(ElementTheme value)
        {
            UpdateFontColor();
        }

        #endregion
    }
}
