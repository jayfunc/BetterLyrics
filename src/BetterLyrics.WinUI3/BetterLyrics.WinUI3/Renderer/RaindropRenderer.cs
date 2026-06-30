using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class RaindropRenderer : EffectRendererBase, IDisposable
    {
        private PixelShaderEffect<RaindropEffect>? _raindropEffect;
        private float _timeAccumulator = 0f;

        public bool IsEnabled { get; set; } = false;
        public float RainSpeed { get; set; } = 0;
        public float RainSize { get; set; } = 0;
        public float RainDensity { get; set; } = 0;
        public float LightAngle { get; set; } = 0;
        public float ShadowIntensity { get; set; } = 0;

        public void LoadResources()
        {
            Dispose();
            _raindropEffect = new PixelShaderEffect<RaindropEffect>();
        }

        public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity, bool is3DEnabled)
        {
            if (_raindropEffect == null || !IsEnabled) return;
            base.UpdateBreathing(bassEnergy, breathingIntensity);
            _timeAccumulator += (float)deltaTime.TotalSeconds;

            if (is3DEnabled)
            {
                Vector3 center = new Vector3((float)control.Size.Width / 2, (float)control.Size.Height / 2, 0);
                base.UpdateParallaxMatrix(center, isAutoParallax: true);
            }
            else
            {
                base.ResetParallaxMatrix();
            }
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
        {
            if (_raindropEffect == null || !IsEnabled) return;

            float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

            var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

            _raindropEffect.ConstantBuffer = new RaindropEffect(
                _timeAccumulator,
                new float2(width, height),
                RainSpeed,
                RainSize,
                RainDensity,
                LightAngle,
                ShadowIntensity
            );

            ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

            base.DrawWithParallax(ds, _raindropEffect);

            ResetTransform(ds, isBreathingEffectEnabled);
        }

        public void Dispose()
        {
            _raindropEffect?.Dispose();
            _raindropEffect = null;
        }
    }
}