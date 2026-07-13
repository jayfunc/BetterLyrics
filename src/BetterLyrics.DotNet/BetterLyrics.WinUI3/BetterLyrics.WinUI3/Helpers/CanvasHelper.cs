using System;
using Windows.Graphics.Effects;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace BetterLyrics.WinUI3.Helpers;

public class CanvasHelper
{
    public static ShadowEffect CreateForegroundShadowEffect(CanvasCommandList foregroundFontEffect,
        IGraphicsEffectSource mask, Color shadowColor, double shadowAmount)
    {
        return new ShadowEffect
        {
            Source = new AlphaMaskEffect
            {
                Source = foregroundFontEffect,
                AlphaMask = mask
            },
            ShadowColor = shadowColor,
            BlurAmount = (float)Math.Clamp(shadowAmount, 0, 100),
            Optimization = EffectOptimization.Speed
        };
    }
}