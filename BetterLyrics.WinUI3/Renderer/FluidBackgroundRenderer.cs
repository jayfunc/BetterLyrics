using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class FluidBackgroundRenderer : BreathingRendererBase, IDisposable
    {
        private PixelShaderEffect<FluidBackgroundEffect>? _fluidEffect;
        private float _timeAccumulator = 0f;

        private float3 _c1, _c2, _c3, _c4;

        public bool IsEnabled { get; set; } = false;
        public double Opacity { get; set; } = 1.0;
        public bool EnableLightWave { get; set; } = true;
        public bool UseHSVBlending { get; set; } = false;

        private float _rnd1 = 0, _rnd2 = 0, _rnd3 = 0;

        public void LoadResources()
        {
            Dispose();
            _fluidEffect = new PixelShaderEffect<FluidBackgroundEffect>();
        }

        public void UpdateColors(Color c1, Color c2, Color c3, Color c4)
        {
            Vector3 v1 = c1.ToVector3RGB();
            Vector3 v2 = c2.ToVector3RGB();
            Vector3 v3 = c3.ToVector3RGB();
            Vector3 v4 = c4.ToVector3RGB();

            _c1 = new float3(v1.X, v1.Y, v1.Z);
            _c2 = new float3(v2.X, v2.Y, v2.Z);
            _c3 = new float3(v3.X, v3.Y, v3.Z);
            _c4 = new float3(v4.X, v4.Y, v4.Z);
        }

        public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity)
        {
            if (_fluidEffect == null || !IsEnabled) return;

            base.UpdateBreathing(bassEnergy, breathingIntensity);

            _timeAccumulator += (float)deltaTime.TotalSeconds;
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
        {
            if (_fluidEffect == null || !IsEnabled || Opacity <= 0) return;

            float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

            _fluidEffect.ConstantBuffer = new FluidBackgroundEffect(
                new float2(width, height),
                _timeAccumulator,
                _c1, _c2, _c3, _c4,
                _rnd1, _rnd2, _rnd3,
                UseHSVBlending,
                EnableLightWave
            );

            var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

            ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

            if (Opacity >= 1.0)
            {
                ds.DrawImage(_fluidEffect);
            }
            else
            {
                using var opacityEffect = new OpacityEffect
                {
                    Source = _fluidEffect,
                    Opacity = (float)Opacity
                };
                ds.DrawImage(opacityEffect);
            }

            ResetTransform(ds, isBreathingEffectEnabled);
        }

        public void Dispose()
        {
            _fluidEffect?.Dispose();
            _fluidEffect = null;
        }
    }
}