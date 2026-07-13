using System;
using System.Numerics;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer;

public partial class SnowRenderer : EffectRendererBase, IDisposable
{
    private PixelShaderEffect<SnowEffect>? _snowEffect;
    private float _timeAccumulator;

    public bool IsEnabled { get; set; } = false;
    public float Amount { get; set; } = 0.5f;
    public float Speed { get; set; } = 1.0f;

    public void Dispose()
    {
        _snowEffect?.Dispose();
        _snowEffect = null;
    }

    public void LoadResources()
    {
        Dispose();
        _snowEffect = new PixelShaderEffect<SnowEffect>();
    }

    public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled)
    {
        if (_snowEffect == null || !IsEnabled) return;

        UpdateBreathing(bassEnergy, breathingIntensity);
        _timeAccumulator += (float)deltaTime.TotalSeconds;

        if (is3DEnabled)
        {
            var center = new Vector3((float)control.Size.Width / 2, (float)control.Size.Height / 2, 0);
            UpdateParallaxMatrix(center, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
    {
        if (_snowEffect == null || !IsEnabled) return;

        float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
        float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

        var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

        _snowEffect.ConstantBuffer = new SnowEffect(
            _timeAccumulator,
            new float2(width, height),
            Amount, // 0.0 ~ 1.0
            Speed
        );

        ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

        DrawWithParallax(ds, _snowEffect);

        ResetTransform(ds, isBreathingEffectEnabled);
    }
}