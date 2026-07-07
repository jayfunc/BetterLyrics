using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class RaindropRenderer : EffectRendererBase, IDisposable
{
    private SKRuntimeEffect? _raindropEffect;
    private float _timeAccumulator;

    public bool IsEnabled { get; set; } = false;
    public float RainSpeed { get; set; } = 0;
    public float RainSize { get; set; } = 0;
    public float RainDensity { get; set; } = 0;
    public float LightAngle { get; set; } = 0;
    public float ShadowIntensity { get; set; } = 0;

    // TODO: Paste your translated SkSL raindrop shader code here.
    private const string SkSlShaderCode = @"
        uniform vec2 u_resolution;
        uniform float u_time;
        uniform float u_rainSpeed;
        uniform float u_rainSize;
        uniform float u_rainDensity;
        uniform float u_lightAngle;
        uniform float u_shadowIntensity;

        vec4 main(vec2 fragCoord) {
            // Your raindrop math/logic here
            vec2 uv = fragCoord / u_resolution.xy;
            return vec4(0.0, 0.0, 0.0, 0.0); // Placeholder
        }
    ";

    public void Dispose()
    {
        _raindropEffect?.Dispose();
        _raindropEffect = null;
    }

    public void LoadResources()
    {
        Dispose();

        var result = SKRuntimeEffect.CreateShader(SkSlShaderCode, out var error);
        if (result == null)
        {
            throw new InvalidOperationException($"Raindrop shader compilation failed: {error}");
        }
        _raindropEffect = result;
    }

    public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled, Size controlSize)
    {
        if (_raindropEffect == null || !IsEnabled) return;

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
        if (_raindropEffect == null || !IsEnabled) return;

        context.Custom(new RaindropCustomDrawOperation(this, bounds, isBreathingEffectEnabled));
    }

    private void RenderSkia(SKCanvas canvas, Rect bounds, bool isBreathingEffectEnabled)
    {
        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        // Map all properties to the Skia uniform block
        var uniforms = new SKRuntimeEffectUniforms(_raindropEffect!)
        {
            { "u_time", _timeAccumulator },
            { "u_resolution", new[] { width, height } },
            { "u_rainSpeed", RainSpeed },
            { "u_rainSize", RainSize },
            { "u_rainDensity", RainDensity },
            { "u_lightAngle", LightAngle },
            { "u_shadowIntensity", ShadowIntensity }
        };

        using var shader = _raindropEffect!.ToShader(uniforms);
        using var paint = new SKPaint
        {
            Shader = shader,
            IsAntialias = true
        };

        canvas.Save();

        var center = new Vector2(width / 2, height / 2);

        ApplyBreathingTransform(canvas, center, isBreathingEffectEnabled);

        // Draw the raindrop shader across the bounds
        DrawWithParallax(canvas, width, height, paint);

        ResetTransform(canvas, isBreathingEffectEnabled);

        canvas.Restore();
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class RaindropCustomDrawOperation : ICustomDrawOperation
    {
        private readonly RaindropRenderer _renderer;
        private readonly bool _isBreathingEnabled;

        public Rect Bounds { get; }

        public RaindropCustomDrawOperation(RaindropRenderer renderer, Rect bounds, bool isBreathingEnabled)
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