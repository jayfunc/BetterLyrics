using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class FluidBackgroundRenderer : EffectRendererBase, IDisposable
{
    private readonly float _rnd1 = 0;
    private readonly float _rnd2 = 0;
    private readonly float _rnd3 = 0;

    private Vector3 _c1 = Vector3.Zero, _c2 = Vector3.Zero, _c3 = Vector3.Zero, _c4 = Vector3.Zero;

    private SKImage? _cachedImage;
    private SKRuntimeEffect? _fluidEffect;
    private float _timeAccumulator;

    public bool IsEnabled { get; set; } = false;
    public double Opacity { get; set; } = 1.0;
    public bool EnableLightWave { get; set; } = true;
    public bool UseHSVBlending { get; set; } = false;
    public bool EnableDithering { get; set; } = false;
    public bool IsStatic { get; set; } = false;

    public void Dispose()
    {
        _fluidEffect?.Dispose();
        _fluidEffect = null;

        _cachedImage?.Dispose();
        _cachedImage = null;
    }

    public void LoadResources()
    {
        Dispose();

        // Compile the SkSL shader
        var result = SKRuntimeEffect.CreateShader(Shaders.FluidBackgroundEffect.SkSlShaderCode, out var error);
        if (result == null)
        {
            throw new InvalidOperationException($"Shader compilation failed: {error}");
        }
        _fluidEffect = result;
    }

    public void Update(TimeSpan deltaTime, Color c1, Color c2, Color c3, Color c4,
        float bassEnergy, int breathingIntensity, bool is3DEnabled, Size controlSize)
    {
        if (_fluidEffect == null || !IsEnabled) return;

        // Convert Avalonia Color (0-255) to Skia uniform format (0.0-1.0)
        _c1 = new Vector3(c1.R / 255f, c1.G / 255f, c1.B / 255f);
        _c2 = new Vector3(c2.R / 255f, c2.G / 255f, c2.B / 255f);
        _c3 = new Vector3(c3.R / 255f, c3.G / 255f, c3.B / 255f);
        _c4 = new Vector3(c4.R / 255f, c4.G / 255f, c4.B / 255f);

        UpdateBreathing(bassEnergy, breathingIntensity);

        if (!IsStatic) _timeAccumulator += (float)deltaTime.TotalSeconds;

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
        if (_fluidEffect == null || !IsEnabled || Opacity <= 0) return;

        // Dispatch a custom Skia draw operation
        context.Custom(new FluidCustomDrawOperation(this, bounds, isBreathingEffectEnabled));
    }

    private void RenderSkia(SKCanvas canvas, Rect bounds, bool isBreathingEffectEnabled)
    {
        float width = (float)bounds.Width;
        float height = (float)bounds.Height;

        // Map constants to Skia uniforms
        var uniforms = new SKRuntimeEffectUniforms(_fluidEffect!)
        {
            { "u_resolution", new[] { width, height } },
            { "u_time", _timeAccumulator },
            { "u_c1", new[] { _c1.X, _c1.Y, _c1.Z } },
            { "u_c2", new[] { _c2.X, _c2.Y, _c2.Z } },
            { "u_c3", new[] { _c3.X, _c3.Y, _c3.Z } },
            { "u_c4", new[] { _c4.X, _c4.Y, _c4.Z } },
            { "u_rnd", new[] { _rnd1, _rnd2, _rnd3 } },
            { "u_flags", new[] { UseHSVBlending ? 1f:0f, EnableLightWave ? 1f:0f, EnableDithering ? 1f:0f } }
        };

        using var shader = _fluidEffect!.ToShader(uniforms);

        // Handle global opacity at the Paint level
        byte alpha = (byte)(Opacity * 255);
        using var paint = new SKPaint
        {
            Shader = shader,
            Color = new SKColor(255, 255, 255, alpha),
            IsAntialias = true
        };

        canvas.Save();

        var center = new Vector2(width / 2, height / 2);

        // Note: You'll need to adapt these methods in your base class to accept SKCanvas 
        // instead of CanvasDrawingSession, utilizing canvas.Translate() and canvas.Scale().
        ApplyBreathingTransform(canvas, center, isBreathingEffectEnabled);

        if (IsStatic)
        {
            if (_cachedImage == null || _cachedImage.Width != (int)width || _cachedImage.Height != (int)height)
            {
                _cachedImage?.Dispose();
                var info = new SKImageInfo((int)width, (int)height);
                using var surface = SKSurface.Create(info);

                // Draw shader to cache surface
                surface.Canvas.DrawRect(0, 0, width, height, paint);
                _cachedImage = surface.Snapshot();
            }

            using var imagePaint = new SKPaint { Color = new SKColor(255, 255, 255, alpha) };
            DrawWithParallax(canvas, _cachedImage, imagePaint);
        }
        else
        {
            if (_cachedImage != null)
            {
                _cachedImage.Dispose();
                _cachedImage = null;
            }

            // Draw shader directly to screen
            DrawWithParallax(canvas, width, height, paint);
        }

        ResetTransform(canvas, isBreathingEffectEnabled);
        canvas.Restore();
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class FluidCustomDrawOperation : ICustomDrawOperation
    {
        private readonly FluidBackgroundRenderer _renderer;
        private readonly bool _isBreathingEnabled;

        public Rect Bounds { get; }

        public FluidCustomDrawOperation(FluidBackgroundRenderer renderer, Rect bounds, bool isBreathingEnabled)
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

            // Lease the SKCanvas natively from Avalonia's pipeline
            using var lease = leaseFeature.Lease();
            _renderer.RenderSkia(lease.SkCanvas, Bounds, _isBreathingEnabled);
        }
    }
}