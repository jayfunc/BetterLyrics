using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class SnowRenderer : EffectRendererBase, IDisposable
{
    private SKRuntimeEffect? _snowEffect;
    private float _timeAccumulator;

    public bool IsEnabled { get; set; } = false;
    public float Amount { get; set; } = 0.5f;
    public float Speed { get; set; } = 1.0f;

    // TODO: Paste your translated SkSL snow shader code here.
    private const string SkSlShaderCode = @"
        uniform vec2 u_resolution;
        uniform float u_time;
        uniform float u_amount;
        uniform float u_speed;

        vec4 main(vec2 fragCoord) {
            // Your snow rendering math here
            vec2 uv = fragCoord / u_resolution.xy;
            return vec4(1.0, 1.0, 1.0, 0.0); // Placeholder return
        }
    ";

    public void Dispose()
    {
        _snowEffect?.Dispose();
        _snowEffect = null;
    }

    public void LoadResources()
    {
        Dispose();

        var result = SKRuntimeEffect.CreateShader(SkSlShaderCode, out var error);
        if (result == null)
        {
            throw new InvalidOperationException($"Snow shader compilation failed: {error}");
        }
        _snowEffect = result;
    }

    public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled, Size controlSize)
    {
        if (_snowEffect == null || !IsEnabled) return;

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
        if (_snowEffect == null || !IsEnabled) return;

        context.Custom(new SnowCustomDrawOperation(this, bounds, isBreathingEffectEnabled));
    }

    private void RenderSkia(SKCanvas canvas, Rect bounds, bool isBreathingEffectEnabled)
    {
        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        // Map properties to the Skia uniform block
        var uniforms = new SKRuntimeEffectUniforms(_snowEffect!)
        {
            { "u_time", _timeAccumulator },
            { "u_resolution", new[] { width, height } },
            { "u_amount", Amount },
            { "u_speed", Speed }
        };

        using var shader = _snowEffect!.ToShader(uniforms);
        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true
        };

        canvas.Save();

        var center = new Vector2(width / 2, height / 2);

        ApplyBreathingTransform(canvas, center, isBreathingEffectEnabled);

        // Draw the snow shader across the bounds
        DrawWithParallax(canvas, width, height, paint);

        ResetTransform(canvas, isBreathingEffectEnabled);

        canvas.Restore();
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class SnowCustomDrawOperation : ICustomDrawOperation
    {
        private readonly SnowRenderer _renderer;
        private readonly bool _isBreathingEnabled;

        public Rect Bounds { get; }

        public SnowCustomDrawOperation(SnowRenderer renderer, Rect bounds, bool isBreathingEnabled)
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