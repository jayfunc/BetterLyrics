using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Windows.Graphics.Imaging;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class InAppLyricsRendererViewModel
        : BaseLyricsRendererViewModel,
            IRecipient<PropertyChangedMessage<int>>,
            IRecipient<PropertyChangedMessage<float>>,
            IRecipient<PropertyChangedMessage<double>>,
            IRecipient<PropertyChangedMessage<bool>>,
            IRecipient<PropertyChangedMessage<InAppLyricsDisplayType>>,
            IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
            IRecipient<PropertyChangedMessage<LyricsAlignmentType>>
    {
        public InAppLyricsDisplayType DisplayType { get; set; }

        private float _rotateAngle = 0f;

        [ObservableProperty]
        public override partial SongInfo? SongInfo { get; set; }

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

        private float _transitionAlpha = 1f;
        private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(1000);
        private DateTimeOffset _transitionStartTime;
        private bool _isTransitioning = false;

        private readonly float _coverRotateSpeed = 0.003f;

        public InAppLyricsRendererViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService, playbackService)
        {
            CoverImageRadius = _settingsService.CoverImageRadius;
            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            LyricsFontColorType = _settingsService.InAppLyricsFontColorType;
            LyricsAlignmentType = _settingsService.InAppLyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.InAppLyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.InAppLyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.InAppLyricsFontSize;
            LyricsBlurAmount = _settingsService.InAppLyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsInAppLyricsGlowEffectEnabled;
            IsLyricsDynamicGlowEffectEnabled =
                _settingsService.IsInAppLyricsDynamicGlowEffectEnabled;

            UpdateFontColor();
        }

        async partial void OnSongInfoChanged(SongInfo? value)
        {
            if (value?.AlbumArt is byte[] bytes)
                SoftwareBitmap = await (
                    await ImageHelper.GetDecoderFromByte(bytes)
                ).GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }

        public override void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (!IsCoverOverlayEnabled || SoftwareBitmap == null)
                return;

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

            switch (DisplayType)
            {
                case InAppLyricsDisplayType.AlbumArtOnly:
                case InAppLyricsDisplayType.PlaceholderOnly:
                    break;
                case InAppLyricsDisplayType.LyricsOnly:
                case InAppLyricsDisplayType.SplitView:
                    base.Draw(control, ds);
                    break;
                default:
                    break;
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

        public override void Calculate(
            ICanvasAnimatedControl control,
            CanvasAnimatedUpdateEventArgs args
        )
        {
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

            base.Calculate(control, args);
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
            else if (message.Sender is InAppLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.IsLyricsGlowEffectEnabled)
                )
                {
                    IsLyricsGlowEffectEnabled = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.IsLyricsDynamicGlowEffectEnabled)
                )
                {
                    IsLyricsDynamicGlowEffectEnabled = message.NewValue;
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
            else if (message.Sender is InAppLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsVerticalEdgeOpacity)
                )
                {
                    LyricsVerticalEdgeOpacity = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsBlurAmount)
                )
                {
                    LyricsBlurAmount = message.NewValue;
                }
                else if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsFontSize)
                )
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender is InAppLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsFontColorType)
                )
                {
                    LyricsFontColorType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsAlignmentType> message)
        {
            if (message.Sender is InAppLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsAlignmentType)
                )
                {
                    LyricsAlignmentType = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<float> message)
        {
            if (message.Sender is InAppLyricsSettingsControlViewModel)
            {
                if (
                    message.PropertyName
                    == nameof(InAppLyricsSettingsControlViewModel.LyricsLineSpacingFactor)
                )
                {
                    LyricsLineSpacingFactor = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender is InAppLyricsPageViewModel)
            {
                if (message.PropertyName == nameof(InAppLyricsPageViewModel.LimitedLineWidth))
                {
                    LimitedLineWidth = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<InAppLyricsDisplayType> message)
        {
            DisplayType = message.NewValue;
        }
    }
}
