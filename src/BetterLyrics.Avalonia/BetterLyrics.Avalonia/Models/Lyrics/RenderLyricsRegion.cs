using System;
using Avalonia;
using Avalonia.Media;
using BetterLyrics.Avalonia.Extensions; // Or wherever your generic extensions are

namespace BetterLyrics.Avalonia.Models.Lyrics;

public partial class RenderLyricsRegion : IDisposable
{
    // In the previous conversion, ICanvasImage caches became DrawingGroup
    public RenderLyricsRegion(DrawingGroup cachedFill, DrawingGroup? cachedStroke)
    {
        CachedFillMask = cachedFill;
        CachedStrokeMask = cachedStroke;

        // Pre-populate the stops to mirror the fixed-size array behavior of the original
        for (int i = 0; i < 4; i++)
        {
            FillStops[i] = new GradientStop();
            StrokeStops[i] = new GradientStop();
        }
    }

    // CanvasGradientStop is replaced by Avalonia.Media.GradientStop
    public GradientStop[] FillStops { get; } = new GradientStop[4];
    public GradientStop[] StrokeStops { get; } = new GradientStop[4];

    // Hold onto the masks instead of Win2D Effect nodes
    public DrawingGroup CachedFillMask { get; }
    public DrawingGroup? CachedStrokeMask { get; }

    /// <summary>
    /// Replaces the Win2D CompositeEffect pipeline. Call this from your rendering loop.
    /// </summary>
    public void Render(DrawingContext context, Rect bounds, Point gradientStart, Point gradientEnd)
    {
        // 1. Draw the Stroke Layer (SourceOver bottom layer)
        if (CachedStrokeMask != null)
        {
            var strokeBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(gradientStart, RelativeUnit.Absolute),
                EndPoint = new RelativePoint(gradientEnd, RelativeUnit.Absolute),
                GradientStops = new GradientStops()
            };
            strokeBrush.GradientStops.AddRange(StrokeStops);

            var strokeMaskBrush = new DrawingBrush(CachedStrokeMask) { Stretch = Stretch.None };

            // Equivalent to AlphaMaskEffect for the stroke
            using (context.PushOpacityMask(strokeMaskBrush, bounds))
            {
                context.DrawRectangle(strokeBrush, null, bounds);
            }
        }

        // 2. Draw the Fill Layer (SourceOver top layer)
        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(gradientStart, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(gradientEnd, RelativeUnit.Absolute),
            GradientStops = new GradientStops()
        };
        fillBrush.GradientStops.AddRange(FillStops);

        var fillMaskBrush = new DrawingBrush(CachedFillMask) { Stretch = Stretch.None };

        // Equivalent to AlphaMaskEffect for the fill
        using (context.PushOpacityMask(fillMaskBrush, bounds))
        {
            context.DrawRectangle(fillBrush, null, bounds);
        }
    }

    public void Dispose()
    {
        // DrawingGroup and GradientStops in Avalonia are not IDisposable.
        // Memory is managed via standard GC and the Skia backend lifecycle.
    }
}