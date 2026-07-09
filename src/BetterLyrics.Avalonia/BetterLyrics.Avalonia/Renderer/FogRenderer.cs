using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class FogRenderer : EffectRendererBase, IDisposable // Assuming EffectRendererBase adaptation is handled
{
    private SKRuntimeEffect? _fogEffect;
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

        var result = SKRuntimeEffect.CreateShader(Shaders.FogEffect.SkSlShaderCode, out var error);
        if (result == null)
        {
            throw new InvalidOperationException($"Fog shader compilation failed: {error}");
        }
        _fogEffect = result;
    }

    public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled, Size controlSize)
    {
        if (_fogEffect == null || !IsEnabled) return;

        UpdateBreathing(bassEnergy, breathingIntensity);
        _timeAccumulator += (float)deltaTime.TotalSeconds;

        if (is3DEnabled)
        {
            var center = new Vector3((float)controlSize.Width / 2, (float)controlSize.Height / 2, 0);
            UpdateParallaxMatrix(center, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    // Avalonia DrawingContext entry point
    public void Draw(DrawingContext context, Rect bounds, bool isBreathingEffectEnabled)
    {
        if (_fogEffect == null || !IsEnabled) return;

        context.Custom(new FogCustomDrawOperation(this, bounds, isBreathingEffectEnabled));
    }

    private void RenderSkia(SKCanvas canvas, Rect bounds, bool isBreathingEffectEnabled)
    {
        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        var uniforms = new SKRuntimeEffectUniforms(_fogEffect!)
        {
            { "u_resolution", new[] { width, height } },
            { "u_time", _timeAccumulator }
        };

        using var shader = _fogEffect!.ToShader(uniforms);
        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true
        };

        canvas.Save();

        var center = new Vector2(width / 2, height / 2);

        // Note: Adapt these base class methods to accept SKCanvas
        ApplyBreathingTransform(canvas, center, isBreathingEffectEnabled);

        // Draw the fog shader across the bounds
        DrawWithParallax(canvas, width, height, paint);

        ResetTransform(canvas, isBreathingEffectEnabled);

        canvas.Restore();
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class FogCustomDrawOperation : ICustomDrawOperation
    {
        private readonly FogRenderer _renderer;
        private readonly bool _isBreathingEnabled;

        public Rect Bounds { get; }

        public FogCustomDrawOperation(FogRenderer renderer, Rect bounds, bool isBreathingEnabled)
        {
            _renderer = renderer;
            Bounds = bounds;
            _isBreathingEnabled = isBreathingEnabled;
        }

        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            _renderer.RenderSkia(lease.SkCanvas, Bounds, _isBreathingEnabled);
        }
    }
}