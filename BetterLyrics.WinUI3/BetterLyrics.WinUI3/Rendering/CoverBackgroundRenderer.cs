using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics.Imaging;

namespace BetterLyrics.WinUI3.Rendering
{
    public class CoverBackgroundRenderer
    {
        private readonly SettingsService _settingsService;

        public float RotateAngle { get; set; } = 0f;

        private SoftwareBitmap? _lastSoftwareBitmap = null;
        private SoftwareBitmap? _softwareBitmap = null;
        public SoftwareBitmap? SoftwareBitmap
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

        private float _transitionAlpha = 1f;
        private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(1000);
        private DateTimeOffset _transitionStartTime;
        private bool _isTransitioning = false;

        public CoverBackgroundRenderer()
        {
            _settingsService = Ioc.Default.GetService<SettingsService>()!;
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (!_settingsService.IsCoverOverlayEnabled || SoftwareBitmap == null)
                return;

            ds.Transform = Matrix3x2.CreateRotation(RotateAngle, control.Size.ToVector2() * 0.5f);

            var overlappedCovers = new CanvasCommandList(control);
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
                Opacity = _settingsService.CoverOverlayOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = _settingsService.CoverOverlayBlurAmount,
                    Source = overlappedCovers,
                },
            };
            ds.DrawImage(coverOverlayEffect);

            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawImgae(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            SoftwareBitmap softwareBitmap,
            float opacity
        )
        {
            float imageWidth = (float)(softwareBitmap.PixelWidth * 96f / softwareBitmap.DpiX);
            float imageHeight = (float)(softwareBitmap.PixelHeight * 96f / softwareBitmap.DpiY);
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
                        Source = CanvasBitmap.CreateFromSoftwareBitmap(control, softwareBitmap),
                    },
                    Opacity = opacity,
                },
                (float)control.Size.Width / 2 - imageWidth * scaleFactor / 2,
                (float)control.Size.Height / 2 - imageHeight * scaleFactor / 2
            );
        }

        public void Calculate(ICanvasAnimatedControl control)
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
        }
    }
}
