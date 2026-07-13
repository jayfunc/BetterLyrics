using System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;

namespace BetterLyrics.WinUI3.Models.Lyrics;

public partial class RenderLyricsRegion : IDisposable
{
    public RenderLyricsRegion(ICanvasImage cachedFill, ICanvasImage? cachedStroke)
    {
        FinalFillEffect = new AlphaMaskEffect { AlphaMask = cachedFill };

        if (cachedStroke != null)
        {
            FinalStrokeEffect = new AlphaMaskEffect { AlphaMask = cachedStroke };
            CombinedEffect = new CompositeEffect
            {
                Sources = { FinalStrokeEffect, FinalFillEffect },
                Mode = CanvasComposite.SourceOver
            };
        }
    }

    public CanvasGradientStop[] FillStops { get; } = new CanvasGradientStop[4];
    public CanvasGradientStop[] StrokeStops { get; } = new CanvasGradientStop[4];

    public AlphaMaskEffect FinalFillEffect { get; }
    public AlphaMaskEffect? FinalStrokeEffect { get; }
    public CompositeEffect? CombinedEffect { get; }

    public void Dispose()
    {
        FinalFillEffect?.Dispose();
        FinalStrokeEffect?.Dispose();
        CombinedEffect?.Dispose();
    }
}