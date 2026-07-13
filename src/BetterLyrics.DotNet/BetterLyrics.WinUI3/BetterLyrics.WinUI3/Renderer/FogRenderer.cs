using System;
using System.Numerics;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer;

public partial class FogRenderer : EffectRendererBase, IDisposable
{
    private PixelShaderEffect<FogEffect>? _fogEffect;
    private float _timeAccumulator;

    public bool IsEnabled { get; set; } = false;

    public void Dispose()
    {
        _fogEffect?.Dispose();
        _fogEffect = null;
    }

    public void LoadResources()
    {
        Dispose();
        _fogEffect = new PixelShaderEffect<FogEffect>();
    }

    public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled)
    {
        if (_fogEffect == null || !IsEnabled) return;
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
        if (_fogEffect == null || !IsEnabled) return;

        float width = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
        float height = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

        var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

        _fogEffect.ConstantBuffer = new FogEffect(
            _timeAccumulator,
            new float2(width, height)
        );

        ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

        DrawWithParallax(ds, _fogEffect);

        ResetTransform(ds, isBreathingEffectEnabled);
    }
}