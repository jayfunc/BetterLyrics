using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Helpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Lyrics;

public class RenderLyricsChar : BaseRenderLyricsChar
{
    private ColorMatrixEffect? _hdrGlowColor;
    private MorphologyEffect? _hdrGlowSpread;
    private GaussianBlurEffect? _hdrGlow;
    private float _hdrHighlightGain;
    private Color _hdrTint;

    public RenderLyricsChar(BaseLyrics lyricsChars, AppRect layoutRect) : base(lyricsChars, layoutRect)
    {
        Crop = new CropEffect { BorderMode = EffectBorderMode.Hard };
        Glow = new GaussianBlurEffect { Source = Crop, BorderMode = EffectBorderMode.Soft };
    }

    public CropEffect Crop { get; }
    public GaussianBlurEffect Glow { get; }

    public (ICanvasImage Fill, float Strength) DrawHdrGlow(CanvasDrawingSession ds, Rect sourceRect,
        Rect destinationRect, float gain, Color tint)
    {
        if (_hdrGlowColor == null)
        {
            _hdrGlowColor = CreateHdrTintEffect(Crop, gain, tint);
            _hdrHighlightGain = gain;
            _hdrTint = tint;
        }
        else if (_hdrHighlightGain != gain || !_hdrTint.Equals(tint))
        {
            _hdrHighlightGain = gain;
            _hdrTint = tint;
            _hdrGlowColor.ColorMatrix = GetHdrTintMatrix(gain, tint);
        }

        // Give the bright inner halo room outside the glyph before feathering its edge.
        _hdrGlowSpread ??= new MorphologyEffect
        {
            Source = _hdrGlowColor,
            Mode = MorphologyEffectMode.Dilate,
            Width = 5,
            Height = 5,
            BufferPrecision = CanvasBufferPrecision.Precision16Float
        };
        _hdrGlow ??= new GaussianBlurEffect
        {
            Source = _hdrGlowSpread,
            BorderMode = EffectBorderMode.Soft,
            BufferPrecision = CanvasBufferPrecision.Precision16Float
        };

        _hdrGlow.BlurAmount = Glow.BlurAmount;
        var strength = EasingHelper.SmoothStep(Math.Clamp(Glow.BlurAmount / 4, 0, 1));
        var passed = EasingHelper.SmoothStep(Math.Clamp((float)(ProgressPlayed - 0.65) / 0.35f, 0, 1));
        ds.DrawImage(_hdrGlow, destinationRect, sourceRect, strength * (0.82f - 0.59f * passed));
        return (_hdrGlowColor, strength);
    }

    internal static ColorMatrixEffect CreateHdrTintEffect(ICanvasImage source, float gain, Color tint) =>
        new()
        {
            Source = source,
            ClampOutput = false,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
            ColorMatrix = GetHdrTintMatrix(gain, tint)
        };

    private static Matrix5x4 GetHdrTintMatrix(float gain, Color tint) =>
        new()
        {
            M44 = 1,
            M51 = ScaleChannel(tint.R, gain),
            M52 = ScaleChannel(tint.G, gain),
            M53 = ScaleChannel(tint.B, gain)
        };

    private static float ScaleChannel(byte channel, float gain)
    {
        // Apply gain in linear light, then encode it for the sRGB scene.
        var srgb = channel / 255f;
        var linear = srgb <= 0.04045f ? srgb / 12.92f : MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
        linear *= gain;
        return linear <= 0.0031308f ? 12.92f * linear : 1.055f * MathF.Pow(linear, 1 / 2.4f) - 0.055f;
    }

    public void DisposeEffetcts()
    {
        Glow?.Dispose();
        _hdrGlow?.Dispose();
        _hdrGlowSpread?.Dispose();
        _hdrGlowColor?.Dispose();
        Crop?.Dispose();
    }
}
