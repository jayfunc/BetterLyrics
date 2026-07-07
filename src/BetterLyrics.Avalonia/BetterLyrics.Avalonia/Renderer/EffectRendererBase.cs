using System;
using System.Numerics;
using BetterLyrics.Core.Effects;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public abstract class EffectRendererBase
{
    protected float _currentScale = 1.0f;
    private float _targetScale = 1.0f;

    protected Matrix4x4 _threeDimMatrix = Matrix4x4.Identity;
    public ParallaxTiltEffect? ParallaxContext { get; set; }

    protected void UpdateBreathing(float bassEnergy, int intensity)
    {
        if (intensity <= 0)
        {
            _currentScale = 1.0f;
            return;
        }

        var maxScaleOffset = intensity / 100.0f;
        _targetScale = 1.0f + bassEnergy * maxScaleOffset;

        if (_targetScale > _currentScale)
            _currentScale += (_targetScale - _currentScale) * 0.2f;
        else
            _currentScale += (_targetScale - _currentScale) * 0.05f;
    }

    protected void ApplyBreathingTransform(SKCanvas canvas, Vector2 center, bool isEnabled)
    {
        if (isEnabled && _currentScale > 1.0f)
        {
            // Applies scale relative to the center coordinate
            canvas.Scale(_currentScale, _currentScale, center.X, center.Y);
        }
    }

    protected static void ResetTransform(SKCanvas canvas, bool isEnabled)
    {
        // NOTE: In Win2D, ds.Transform = Matrix3x2.Identity resets to absolute zero.
        // In Skia/Avalonia, setting absolute Identity breaks DPI scaling and UI offsets.
        // Because we already wrap the child rendering logic in canvas.Save() and canvas.Restore(),
        // the state is safely reverted. This method is kept as a no-op for API compatibility.
    }

    protected void UpdateParallaxMatrix(Vector3 center, bool isAutoParallax, float manualAngleX = 0,
        float manualAngleY = 0, float manualAngleZ = 0, float depth = 800f)
    {
        float angleX = 0f, angleY = 0f, angleZ = 0f;
        var parallaxTranslation = Matrix4x4.Identity;

        if (isAutoParallax && ParallaxContext != null)
        {
            angleX = ParallaxContext.CurrentRotationX;
            angleY = ParallaxContext.CurrentRotationY;
            parallaxTranslation = Matrix4x4.CreateTranslation(
                ParallaxContext.CurrentTranslateX,
                ParallaxContext.CurrentTranslateY,
                0);
        }
        else
        {
            angleX = manualAngleX;
            angleY = manualAngleY;
            angleZ = manualAngleZ;
        }

        var rotationX = (float)(Math.PI * angleX / 180.0);
        var rotationY = (float)(Math.PI * angleY / 180.0);
        var rotationZ = (float)(Math.PI * angleZ / 180.0);

        var rotation = Matrix4x4.CreateRotationX(rotationX) *
                       Matrix4x4.CreateRotationY(rotationY) *
                       Matrix4x4.CreateRotationZ(rotationZ);

        var perspective = Matrix4x4.Identity;
        if (depth > 0) perspective.M34 = 1.0f / depth;

        _threeDimMatrix = Matrix4x4.CreateTranslation(-center) * rotation * perspective *
                          Matrix4x4.CreateTranslation(center) * parallaxTranslation;
    }

    // Overload 1: For drawing a cached SKImage (used in the IsStatic = true branch)
    protected void DrawWithParallax(SKCanvas canvas, SKImage? source, SKPaint? paint = null)
    {
        if (source == null) return;

        canvas.Save();

        if (!_threeDimMatrix.IsIdentity)
        {
            canvas.Concat(ToSKMatrix(_threeDimMatrix));
        }

        canvas.DrawImage(source, 0, 0, paint);
        canvas.Restore();
    }

    // Overload 2: For drawing a shader effect directly to a rect (used in real-time branch)
    protected void DrawWithParallax(SKCanvas canvas, float width, float height, SKPaint paint)
    {
        canvas.Save();

        if (!_threeDimMatrix.IsIdentity)
        {
            canvas.Concat(ToSKMatrix(_threeDimMatrix));
        }

        canvas.DrawRect(0, 0, width, height, paint);
        canvas.Restore();
    }

    protected void ResetParallaxMatrix()
    {
        _threeDimMatrix = Matrix4x4.Identity;
    }

    /// <summary>
    /// Projects a 3D System.Numerics Matrix4x4 down to a 2D Skia SKMatrix (including perspective).
    /// </summary>
    protected static SKMatrix ToSKMatrix(Matrix4x4 m)
    {
        return new SKMatrix(
            m.M11, m.M21, m.M41, // Scale X, Skew X,  Trans X
            m.M12, m.M22, m.M42, // Skew Y,  Scale Y, Trans Y
            m.M14, m.M24, m.M44  // Persp 0, Persp 1, Persp 2
        );
    }
}