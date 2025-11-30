using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class SnowRenderer : IDisposable
    {
        private PixelShaderEffect<SnowEffect>? _snowEffect;
        private float _timeAccumulator = 0f;

        public bool IsEnabled { get; set; } = false;
        public float Amount { get; set; } = 0.5f;
        public float Speed { get; set; } = 1.0f;

        public void LoadResources()
        {
            Dispose();
            _snowEffect = new PixelShaderEffect<SnowEffect>();
        }

        public void Update(double deltaTime)
        {
            if (_snowEffect == null || !IsEnabled) return;
            _timeAccumulator += (float)deltaTime;
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_snowEffect == null || !IsEnabled) return;

            float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

            _snowEffect.ConstantBuffer = new SnowEffect(
                _timeAccumulator,
                new float2(width, height),
                Amount, // 0.0 ~ 1.0
                Speed
            );

            ds.DrawImage(_snowEffect);
        }

        public void Dispose()
        {
            _snowEffect?.Dispose();
            _snowEffect = null;
        }
    }
}