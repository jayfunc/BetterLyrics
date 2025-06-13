using System;
using System.Numerics;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Windows.Graphics.Imaging;

namespace BetterLyrics.WinUI3.Rendering
{
    public class AlbumArtRenderer
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private float _rotateAngle = 0f;

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

        private float _transitionAlpha = 1f;
        private TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(1000);
        private DateTimeOffset _transitionStartTime;
        private bool _isTransitioning = false;

        private readonly float _coverRotateSpeed = 0.003f;

        private readonly AlbumArtOverlayViewModel _viewModel;

        public AlbumArtRenderer(AlbumArtOverlayViewModel albumArtRendererSettingsViewModel)
        {
            _viewModel = albumArtRendererSettingsViewModel;

            WeakReferenceMessenger.Default.Register<AlbumArtRenderer, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            if (m.Value?.AlbumArt == null) { }
                            else
                            {
                                SoftwareBitmap = await (
                                    await ImageHelper.GetDecoderFromByte(m.Value.AlbumArt)
                                ).GetSoftwareBitmapAsync(
                                    BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Premultiplied
                                );
                            }
                        }
                    );
                }
            );
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (!_viewModel.IsCoverOverlayEnabled || SoftwareBitmap == null)
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
                Opacity = _viewModel.CoverOverlayOpacity / 100f,
                Source = new GaussianBlurEffect
                {
                    BlurAmount = _viewModel.CoverOverlayBlurAmount,
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

            if (_viewModel.IsDynamicCoverOverlay)
            {
                _rotateAngle += _coverRotateSpeed;
                _rotateAngle %= MathF.PI * 2;
            }
        }
    }
}
