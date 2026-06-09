using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class FluidBackgroundRenderer : BreathingRendererBase, IDisposable
    {
        private PixelShaderEffect<FluidBackgroundEffect>? _fluidEffect;
        private float _timeAccumulator = 0f;

        private float3 _c1 = float3.Zero, _c2 = float3.Zero, _c3 = float3.Zero, _c4 = float3.Zero;

        public bool IsEnabled { get; set; } = false;
        public double Opacity { get; set; } = 1.0;
        public bool EnableLightWave { get; set; } = true;
        public bool UseHSVBlending { get; set; } = false;
        public bool EnableDithering { get; set; } = true;
        public bool IsStatic { get; set; } = false;

        private CanvasRenderTarget? _cachedRenderTarget;

        private float _rnd1 = 0, _rnd2 = 0, _rnd3 = 0;

        public void LoadResources()
        {
            Dispose();
            _fluidEffect = new();
        }

        public void Update(TimeSpan deltaTime, Color c1, Color c2, Color c3, Color c4, float bassEnergy, int breathingIntensity)
        {
            if (_fluidEffect == null || !IsEnabled) return;

            Vector3 v1 = c1.ToVector3RGB();
            Vector3 v2 = c2.ToVector3RGB();
            Vector3 v3 = c3.ToVector3RGB();
            Vector3 v4 = c4.ToVector3RGB();

            _c1 = new float3(v1.X, v1.Y, v1.Z);
            _c2 = new float3(v2.X, v2.Y, v2.Z);
            _c3 = new float3(v3.X, v3.Y, v3.Z);
            _c4 = new float3(v4.X, v4.Y, v4.Z);

            base.UpdateBreathing(bassEnergy, breathingIntensity);

            if (!IsStatic)
            {
                _timeAccumulator += (float)deltaTime.TotalSeconds;
            }
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
        {
            if (_fluidEffect == null || !IsEnabled || Opacity <= 0) return;

            float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

            ICanvasImage? sourceToDraw;

            if (IsStatic)
            {
                bool needsUpdateCache = _cachedRenderTarget == null ||
                    _cachedRenderTarget.Size.Width != control.Size.Width ||
                    _cachedRenderTarget.Size.Height != control.Size.Height;

                if (needsUpdateCache)
                {
                    UpdateShaderConstantBuffer(width, height);

                    _cachedRenderTarget?.Dispose();
                    _cachedRenderTarget = new CanvasRenderTarget(control, (float)control.Size.Width, (float)control.Size.Height, control.Dpi);

                    using (var cacheDs = _cachedRenderTarget.CreateDrawingSession())
                    {
                        cacheDs.Clear(Colors.Transparent);
                        cacheDs.DrawImage(_fluidEffect);
                    }
                }

                sourceToDraw = _cachedRenderTarget;
            }
            else
            {
                if (_cachedRenderTarget != null)
                {
                    _cachedRenderTarget.Dispose();
                    _cachedRenderTarget = null;
                }

                UpdateShaderConstantBuffer(width, height);
                sourceToDraw = _fluidEffect;
            }

            var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

            ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

            if (Opacity >= 1.0)
            {
                ds.DrawImage(sourceToDraw);
            }
            else
            {
                using var opacityEffect = new OpacityEffect
                {
                    Source = sourceToDraw,
                    Opacity = (float)Opacity
                };
                ds.DrawImage(opacityEffect);
            }

            ResetTransform(ds, isBreathingEffectEnabled);
        }

        private void UpdateShaderConstantBuffer(float width, float height)
        {
            _fluidEffect!.ConstantBuffer = new FluidBackgroundEffect(
                new float2(width, height),
                _timeAccumulator,
                _c1, _c2, _c3, _c4,
                _rnd1, _rnd2, _rnd3,
                UseHSVBlending,
                EnableLightWave,
                EnableDithering
            );
        }

        public void Dispose()
        {
            _fluidEffect?.Dispose();
            _fluidEffect = null;

            _cachedRenderTarget?.Dispose();
            _cachedRenderTarget = null;
        }
    }
}