using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class CanvasHelper
    {
        public static ShadowEffect CreateForegroundShadowEffect(CanvasCommandList foregroundFontEffect, IGraphicsEffectSource mask, Color shadowColor, double shadowAmount)
        {
            return new ShadowEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = foregroundFontEffect,
                    AlphaMask = mask,
                },
                ShadowColor = shadowColor,
                BlurAmount = (float)Math.Clamp(shadowAmount, 0, 100),
                Optimization = EffectOptimization.Speed,
            };
        }

    }
}
