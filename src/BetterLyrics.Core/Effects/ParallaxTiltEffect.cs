using BetterLyrics.Core.Models.Domain;
using System.Numerics;

namespace BetterLyrics.Core.Effects;

public class ParallaxTiltEffect
{
    private bool _isPointerInside;

    private float _targetRotationX;
    private float _targetRotationY;
    private float _targetTranslateX;
    private float _targetTranslateY;
    public double MaxTiltAngle { get; set; } = 10.0;
    public double ParallaxDepth { get; set; } = 0.0;

    public float CurrentRotationX { get; private set; }
    public float CurrentRotationY { get; private set; }
    public float CurrentTranslateX { get; private set; }
    public float CurrentTranslateY { get; private set; }

    public void OnPointerMoved(AppPoint pointerPos, AppSize canvasSize)
    {
        _isPointerInside = true;

        if (canvasSize.Width == 0 || canvasSize.Height == 0) return;

        var normalizedX = (pointerPos.X - canvasSize.Width / 2) / (canvasSize.Width / 2);
        var normalizedY = (pointerPos.Y - canvasSize.Height / 2) / (canvasSize.Height / 2);

        _targetRotationY = (float)(-normalizedX * MaxTiltAngle);
        _targetRotationX = (float)(normalizedY * MaxTiltAngle);
        _targetTranslateX = (float)(-normalizedX * ParallaxDepth);
        _targetTranslateY = (float)(-normalizedY * ParallaxDepth);
    }

    public void OnPointerExited()
    {
        _isPointerInside = false;

        _targetRotationX = 0;
        _targetRotationY = 0;
        _targetTranslateX = 0;
        _targetTranslateY = 0;
    }

    public bool Update(float lerpFactor = 0.15f)
    {
        var isAnimating = false;

        if (Math.Abs(_targetRotationX - CurrentRotationX) > 0.01f ||
            Math.Abs(_targetRotationY - CurrentRotationY) > 0.01f ||
            Math.Abs(_targetTranslateX - CurrentTranslateX) > 0.01f ||
            Math.Abs(_targetTranslateY - CurrentTranslateY) > 0.01f)
        {
            CurrentRotationX += (_targetRotationX - CurrentRotationX) * lerpFactor;
            CurrentRotationY += (_targetRotationY - CurrentRotationY) * lerpFactor;
            CurrentTranslateX += (_targetTranslateX - CurrentTranslateX) * lerpFactor;
            CurrentTranslateY += (_targetTranslateY - CurrentTranslateY) * lerpFactor;

            isAnimating = true;
        }

        return isAnimating || _isPointerInside;
    }

    public Matrix4x4 GetTransformMatrix(Vector3 center)
    {
        var rotationX = (float)(Math.PI * CurrentRotationX / 180.0);
        var rotationY = (float)(Math.PI * CurrentRotationY / 180.0);

        var rotation = Matrix4x4.CreateRotationX(rotationX) * Matrix4x4.CreateRotationY(rotationY);
        var parallaxTranslation = Matrix4x4.CreateTranslation(CurrentTranslateX, CurrentTranslateY, 0);

        return Matrix4x4.CreateTranslation(-center) *
               rotation *
               Matrix4x4.CreateTranslation(center) * parallaxTranslation;
    }
}