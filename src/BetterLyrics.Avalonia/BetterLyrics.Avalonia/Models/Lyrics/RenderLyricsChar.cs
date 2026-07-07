using System;
using Avalonia;
using Avalonia.Media;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Avalonia.Models.Lyrics;

public class RenderLyricsChar : BaseRenderLyricsChar
{
    public RenderLyricsChar(BaseLyrics lyricsChars, AppRect layoutRect) : base(lyricsChars, layoutRect)
    {
        // 💡 1. 对应原来的 CropEffect。在 Avalonia 中，裁剪通常由 Rect 表达，渲染时使用 PushClip()。
        // 如果你的 AppRect 隐式转换或包含 Avalonia.Rect，可以直接使用。
        CropBounds = new Rect(layoutRect.X, layoutRect.Y, layoutRect.Width, layoutRect.Height);

        // 💡 2. 对应原来的 GaussianBlurEffect。
        // 这里的 Sigma（模糊半径）可以后续动态调整。
        GlowEffect = new BlurEffect();
    }

    /// <summary>
    /// 替代原 CropEffect，供渲染器在 PushClip 时使用
    /// </summary>
    public Rect CropBounds { get; set; }

    /// <summary>
    /// 替代原 GaussianBlurEffect
    /// </summary>
    public BlurEffect GlowEffect { get; }

    /// <summary>
    /// 动态更新模糊半径（对应原 Glow.BlurAmount）
    /// </summary>
    public void UpdateGlowRadius(double radius)
    {
        if (GlowEffect != null)
        {
            GlowEffect.Radius = radius;
        }
    }

    public void DisposeEffetcts()
    {
        // 💡 Avalonia 的 BlurEffect 是托管对象，由 GC 回收，不再需要像 Win2D(COM) 那样手动调用 Dispose()。
    }
}