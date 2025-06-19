using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
        : BaseRendererViewModel,
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
            IRecipient<PropertyChangedMessage<LyricsGlowEffectScope>>
    {
        private protected CanvasTextFormat _textFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };

        public LyricsDisplayType DisplayType { get; set; }

        private float _rotateAngle = 0f;

        private Color ActivatedWindowAccentColor { get; set; } = Colors.Transparent;

        private Color? _albumArtAccentColor = null;

        private bool IsDockMode { get; set; } = false;

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        private List<LyricsLine>? _lyricsForGlowEffect = [];

        private SoftwareBitmap? _lastSoftwareBitmap = null;
        private SoftwareBitmap? _softwareBitmap = null;
        private SoftwareBitmap? SoftwareBitmap
        {
            get => _softwareBitmap;
            set
            {
                if (_softwareBitmap != null)
                {
                    _lastSoftwareBitmap = _softwareBitmap;
                    _transitionStartTime = DateTimeOffset.Now;
                    _isTransitioning = true;
                    _transitionAlpha = 0f;
                }

                _softwareBitmap = value;
            }
        }

        public int CoverImageRadius { get; set; }
        public bool IsCoverOverlayEnabled { get; set; }
        public bool IsDynamicCoverOverlayEnabled { get; set; }
        public int CoverOverlayOpacity { get; set; }
        public int CoverOverlayBlurAmount { get; set; }

        [ObservableProperty]
        public partial bool IsPlaying { get; set; }

        private protected Color _fontColor;

        private Color _lightFontColor = Colors.White;
        private Color _darkFontColor = Colors.Black;

        private readonly float _defaultOpacity = 0.3f;
        private readonly float _highlightedOpacity = 1.0f;

        private readonly float _defaultScale = 0.95f;
        private readonly float _highlightedScale = 1.0f;

        private readonly int _lineEnteringDurationMs = 800;
        private readonly int _lineExitingDurationMs = 800;
        private readonly int _lineScrollDurationMs = 800;

        private float _lastTotalYScroll = 0.0f;
        private float _totalYScroll = 0.0f;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _forceToScroll = false;

        private readonly float _lyricsGlowEffectAmount = 6f;

        private readonly double _rightMargin = 36;
        private readonly float _topMargin = 0f;

        [ObservableProperty]
        public partial double LimitedLineWidth { get; set; }

        private protected bool _isRelayoutNeeded = true;

        [ObservableProperty]
        public partial ElementTheme Theme { get; set; }

        [ObservableProperty]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        [ObservableProperty]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        public LyricsAlignmentType LyricsAlignmentType { get; set; }
        public int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }
        public int LyricsBlurAmount { get; set; }
        public bool IsLyricsGlowEffectEnabled { get; set; }
        public LyricsGlowEffectScope LyricsGlowEffectScope { get; set; }

        private protected readonly IPlaybackService _playbackService;

        private float _transitionAlpha = 1f;
        private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(1000);
        private DateTimeOffset _transitionStartTime;
        private bool _isTransitioning = false;

        private readonly float _coverRotateSpeed = 0.003f;

        private Color _currentBgColor;
        private Color _targetBgColor;
        private float _colorTransitionProgress = 1f;
        private const float ColorTransitionDuration = 0.3f; // 秒
        private bool _isColorTransitioning = false;

        public LyricsRendererViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
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

            _playbackService = playbackService;
            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.PositionChanged += PlaybackService_PositionChanged;

            RefreshPlaybackInfo();

            UpdateFontColor();
        }

        public void RequestRelayout()
        {
            _isRelayoutNeeded = true;
        }

        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            TotalTime = e.Position;
        }

        private void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
        }

        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            IsPlaying = e.IsPlaying;
        }

        public void RefreshPlaybackInfo()
        {
            IsPlaying = _playbackService.IsPlaying;
            SongInfo = _playbackService.SongInfo;
            TotalTime = _playbackService.Position;
        }

        partial void OnLimitedLineWidthChanged(double value)
        {
            _isRelayoutNeeded = true;
        }

        async partial void OnSongInfoChanged(SongInfo? value)
        {
            if (value?.AlbumArt is byte[] bytes)
            {
                SoftwareBitmap = await (
                    await ImageHelper.GetDecoderFromByte(bytes)
                ).GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                _albumArtAccentColor = (
                    await ImageHelper.GetAccentColorsFromByte(bytes)
                ).FirstOrDefault();
            }
            else
            {
                SoftwareBitmap = null;
                _albumArtAccentColor = null;
            }
            UpdateFontColor();
            TotalTime = TimeSpan.Zero;
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _textFormat.FontWeight = value.ToFontWeight();
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _isRelayoutNeeded = true;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            UpdateFontColor();
        }

        partial void OnThemeChanged(ElementTheme value)
        {
            UpdateFontColor();
        }

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

        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < SongInfo?.LyricsLines?.Count; i++)
            {
                var line = SongInfo?.LyricsLines?[i];
                if (line.EndPlayingTimestampMs < TotalTime.TotalMilliseconds)
                {
                    continue;
                }
                return i;
            }

            return -1;
        }

        private Tuple<int, int> GetVisibleLyricsLineIndexBoundaries()
        {
            // _logger.LogDebug($"{_startVisibleLineIndex} {_endVisibleLineIndex}");
            return new Tuple<int, int>(_startVisibleLineIndex, _endVisibleLineIndex);
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (SongInfo == null || SongInfo.LyricsLines == null || SongInfo.LyricsLines.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, SongInfo.LyricsLines.Count - 1);
        }

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

            var currentPlayingLineIndex = GetCurrentPlayingLineIndex();

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
                    (float)LimitedLineWidth,
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
                        centerX += (float)LimitedLineWidth / 2;
                        break;
                    case LyricsAlignmentType.Right:
                        textLayout.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                        centerX += (float)LimitedLineWidth;
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
                        (float)(control.Size.Width - _rightMargin - LimitedLineWidth),
                        _totalYScroll + (float)(control.Size.Height / 2)
                    );

                ds.DrawTextLayout(textLayout, position, Colors.Transparent);
                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

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
                                    _currentBgColor.R,
                                    _currentBgColor.G,
                                    _currentBgColor.B
                                )
                                : _currentBgColor,
                        },
                        new CanvasGradientStop { Position = 1, Color = _currentBgColor },
                    ]
                )
                {
                    StartPoint = new Vector2(0, 0),
                    EndPoint = new Vector2(0, (float)control.Size.Height),
                }
            );
        }

        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            ds.Transform = Matrix3x2.CreateRotation(_rotateAngle, control.Size.ToVector2() * 0.5f);

            var overlappedCovers = new CanvasCommandList(control.Device);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_isTransitioning && _lastSoftwareBitmap != null)
            {
                DrawImgae(control, overlappedCoversDs, _lastSoftwareBitmap, 1 - _transitionAlpha);
                DrawImgae(control, overlappedCoversDs, SoftwareBitmap, _transitionAlpha);
            }
            else
            {
                DrawImgae(control, overlappedCoversDs, SoftwareBitmap, 1);
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

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            bool isAlbumArtOverlayDrawn = IsCoverOverlayEnabled && SoftwareBitmap != null;
            if (isAlbumArtOverlayDrawn)
            {
                DrawAlbumArtBackground(control, ds);
            }

            if (IsDockMode)
            {
                DrawImmersiveBackground(control, ds, isAlbumArtOverlayDrawn);
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
                            SongInfo?.LyricsLines,
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

        private void ReLayout(ICanvasAnimatedControl control)
        {
            if (control == null)
                return;

            _textFormat.FontSize = LyricsFontSize;

            float y = _topMargin;

            // Init Positions
            for (int i = 0; i < SongInfo?.LyricsLines?.Count; i++)
            {
                var line = SongInfo?.LyricsLines?[i];

                // Calculate layout bounds
                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)LimitedLineWidth,
                    (float)control.Size.Height
                );
                line.Position = new Vector2(0, y);

                y +=
                    (float)textLayout.LayoutBounds.Height
                    / textLayout.LineCount
                    * (textLayout.LineCount + LyricsLineSpacingFactor);
            }
        }

        public override void Calculate(
            ICanvasAnimatedControl control,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            base.Calculate(control, args);

            if (_isColorTransitioning)
            {
                _colorTransitionProgress +=
                    (float)ElapsedTime.TotalSeconds / ColorTransitionDuration;
                if (_colorTransitionProgress >= 1f)
                {
                    _colorTransitionProgress = 1f;
                    _isColorTransitioning = false;
                    _currentBgColor = _targetBgColor;
                }
                else
                {
                    _currentBgColor = Helper.ColorHelper.GetInterpolatedColor(
                        _colorTransitionProgress,
                        _currentBgColor,
                        _targetBgColor
                    );
                }
            }

            if (_isTransitioning)
            {
                var elapsed = DateTimeOffset.Now - _transitionStartTime;
                float progress = (float)(
                    elapsed.TotalMilliseconds / _transitionDuration.TotalMilliseconds
                );
                _transitionAlpha = Math.Clamp(progress, 0f, 1f);

                if (_transitionAlpha >= 1f)
                {
                    _isTransitioning = false;
                    _lastSoftwareBitmap?.Dispose();
                    _lastSoftwareBitmap = null;
                }
            }

            if (IsDynamicCoverOverlayEnabled)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }

            if (_isRelayoutNeeded)
            {
                ReLayout(control);
                _isRelayoutNeeded = false;
                _forceToScroll = true;
            }

            int currentPlayingLineIndex = GetCurrentPlayingLineIndex();

            CalculateLinesProps(SongInfo?.LyricsLines, currentPlayingLineIndex, _defaultOpacity);
            CalculateCanvasYScrollOffset(control, currentPlayingLineIndex);

            if (IsLyricsGlowEffectEnabled)
            {
                // Deep copy lyrics lines for glow effect
                _lyricsForGlowEffect = SongInfo?.LyricsLines?.Select(line => line.Clone()).ToList();
                switch (LyricsGlowEffectScope)
                {
                    case LyricsGlowEffectScope.WholeLyrics:
                        break;
                    case LyricsGlowEffectScope.CurrentLine:
                        CalculateLinesProps(_lyricsForGlowEffect, currentPlayingLineIndex, 0);
                        break;
                    case LyricsGlowEffectScope.CurrentChar:
                        CalculateLinesProps(_lyricsForGlowEffect, currentPlayingLineIndex, 0);
                        break;
                    default:
                        break;
                }
            }
        }

        private void CalculateLinesProps(
            List<LyricsLine>? source,
            int currentPlayingLineIndex,
            float defaultOpacity
        )
        {
            var (startLineIndex, endLineIndex) = GetMaxLyricsLineIndexBoundaries();

            for (int i = startLineIndex; source?.Count > 0 && i <= endLineIndex; i++)
            {
                var line = source?[i];

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

                    playProgress =
                        ((float)TotalTime.TotalMilliseconds - line.StartPlayingTimestampMs)
                        / line.DurationMs;

                    var durationFromStartMs =
                        TotalTime.TotalMilliseconds - line.StartPlayingTimestampMs;
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

                        var durationToEndMs =
                            TotalTime.TotalMilliseconds - line.EndPlayingTimestampMs;
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

        private void CalculateCanvasYScrollOffset(
            ICanvasAnimatedControl control,
            int currentPlayingLineIndex
        )
        {
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
            LyricsLine? currentPlayingLine = SongInfo?.LyricsLines?[currentPlayingLineIndex];

            if (currentPlayingLine == null)
            {
                return;
            }

            using var playingTextLayout = new CanvasTextLayout(
                control,
                currentPlayingLine.Text,
                _textFormat,
                (float)LimitedLineWidth,
                (float)control.Size.Height
            );

            var lineScrollingProgress =
                (TotalTime.TotalMilliseconds - currentPlayingLine.StartPlayingTimestampMs)
                / Math.Min(_lineScrollDurationMs, currentPlayingLine.DurationMs);

            float targetYScrollOffset =
                (float?)(
                    -currentPlayingLine.Position.Y
                    + SongInfo?.LyricsLines?[0].Position.Y
                    - playingTextLayout.LayoutBounds.Height / 2
                    - _lastTotalYScroll
                ) ?? 0f;

            var yScrollOffset =
                targetYScrollOffset
                * EasingHelper.SmootherStep((float)Math.Min(1, lineScrollingProgress));

            bool isScrollingNow = lineScrollingProgress <= 1;

            if (isScrollingNow)
            {
                _totalYScroll = _lastTotalYScroll + yScrollOffset;
            }
            else
            {
                if (_forceToScroll && Math.Abs(targetYScrollOffset) >= 1)
                {
                    _totalYScroll = _lastTotalYScroll + targetYScrollOffset;
                    _forceToScroll = false;
                }
                _lastTotalYScroll = _totalYScroll;
            }

            _startVisibleLineIndex = _endVisibleLineIndex = -1;

            // Update visible line indices
            for (
                int i = startLineIndex;
                i >= 0 && i <= endLineIndex && i < SongInfo?.LyricsLines?.Count;
                i++
            )
            {
                var line = SongInfo?.LyricsLines?[i];

                using var textLayout = new CanvasTextLayout(
                    control,
                    line.Text,
                    _textFormat,
                    (float)LimitedLineWidth,
                    (float)control.Size.Height
                );

                if (
                    _totalYScroll
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
                    _totalYScroll
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

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is LyricsPageViewModel)
            {
                if (message.PropertyName == nameof(LyricsPageViewModel.LimitedLineWidth))
                {
                    LimitedLineWidth = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsDisplayType> message)
        {
            DisplayType = message.NewValue;
        }

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

        public void Receive(PropertyChangedMessage<LyricsGlowEffectScope> message)
        {
            if (message.Sender is LyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(LyricsSettingsControlViewModel.LyricsGlowEffectScope)
                )
                {
                    LyricsGlowEffectScope = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.ActivatedWindowAccentColor))
                {
                    _currentBgColor = _isColorTransitioning
                        ? Helper.ColorHelper.GetInterpolatedColor(
                            _colorTransitionProgress,
                            _currentBgColor,
                            _targetBgColor
                        )
                        : ActivatedWindowAccentColor;
                    _targetBgColor = message.NewValue;
                    _colorTransitionProgress = 0f;
                    _isColorTransitioning = true;
                    ActivatedWindowAccentColor = message.NewValue;
                }
            }
        }
    }
}
