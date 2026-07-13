using System;
using System.Numerics;
using BetterLyrics.Core.Effects;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace BetterLyrics.WinUI3.Renderer;

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

    protected void ApplyBreathingTransform(CanvasDrawingSession ds, Vector2 center, bool isEnabled)
    {
        if (isEnabled && _currentScale > 1.0f) ds.Transform = Matrix3x2.CreateScale(_currentScale, center);
    }

    protected static void ResetTransform(CanvasDrawingSession ds, bool isEnabled)
    {
        if (isEnabled) ds.Transform = Matrix3x2.Identity;
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

    protected void DrawWithParallax(CanvasDrawingSession ds, ICanvasImage? source)
    {
        if (source == null) return;

        if (!_threeDimMatrix.IsIdentity)
            ds.DrawImage(new Transform3DEffect
            {
                Source = source,
                TransformMatrix = _threeDimMatrix
            });
        else
            ds.DrawImage(source);
    }

    protected void ResetParallaxMatrix()
    {
        _threeDimMatrix = Matrix4x4.Identity;
    }
}