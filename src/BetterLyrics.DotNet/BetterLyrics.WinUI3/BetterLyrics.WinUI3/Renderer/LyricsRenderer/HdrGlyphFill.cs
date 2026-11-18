using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using System;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

internal sealed class HdrGlyphFill : IDisposable
{
    private readonly ColorSourceEffect _white = new() { Color = Colors.White };
    private readonly CropEffect _playedMask;
    private readonly PixelShaderEffect<HdrGlyphFillEffect> _fill = new()
    {
        BufferPrecision = CanvasBufferPrecision.Precision16Float
    };
    private readonly CompositeEffect? _withStroke;

    public HdrGlyphFill(RenderLyricsRegion region, ICanvasImage? frontMask, Rect bounds)
    {
        _playedMask = new CropEffect { Source = _white, SourceRectangle = bounds };
        _fill.Sources[0] = region.FinalFillEffect;
        _fill.Sources[1] = frontMask ?? region.FinalFillEffect;
        _fill.Sources[2] = _playedMask;
        if (region.FinalStrokeEffect != null)
            _withStroke = new CompositeEffect
            {
                Sources = { region.FinalStrokeEffect, _fill },
                Mode = CanvasComposite.SourceOver
            };
    }

    public ICanvasImage Apply(Rect playedRect, float frontBoost, float glowBoost)
    {
        if (glowBoost > 0) _playedMask.SourceRectangle = playedRect;
        _fill.ConstantBuffer = new HdrGlyphFillEffect(frontBoost, glowBoost);
        return _withStroke ?? (ICanvasImage)_fill;
    }

    public void Dispose()
    {
        _withStroke?.Dispose();
        _fill.Dispose();
        _playedMask.Dispose();
        _white.Dispose();
    }
}
