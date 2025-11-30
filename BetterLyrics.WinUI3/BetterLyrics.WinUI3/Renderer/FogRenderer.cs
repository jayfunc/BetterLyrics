using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class FogRenderer : IDisposable
    {
        private PixelShaderEffect<FogEffect>? _fogEffect;
        private float _timeAccumulator = 0f;

        public bool IsEnabled { get; set; } = false;

        public void LoadResources()
        {
            Dispose();
            _fogEffect = new PixelShaderEffect<FogEffect>();
        }

        public void Update(double deltaTime)
        {
            if (_fogEffect == null || !IsEnabled) return;
            _timeAccumulator += (float)deltaTime;
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_fogEffect == null || !IsEnabled) return;

            float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

           _fogEffect.ConstantBuffer = new FogEffect(
                _timeAccumulator,
                new float2(width, height)
            );

            ds.DrawImage(_fogEffect);
        }

        public void Dispose()
        {
            _fogEffect?.Dispose();
            _fogEffect = null;
        }
    }
}