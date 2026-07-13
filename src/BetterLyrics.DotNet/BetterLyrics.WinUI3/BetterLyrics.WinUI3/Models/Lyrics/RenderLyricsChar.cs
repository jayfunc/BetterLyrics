using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using Microsoft.Graphics.Canvas.Effects;

namespace BetterLyrics.WinUI3.Models.Lyrics;

public class RenderLyricsChar : BaseRenderLyricsChar
{
    public RenderLyricsChar(BaseLyrics lyricsChars, AppRect layoutRect) : base(lyricsChars, layoutRect)
    {
        Crop = new CropEffect { BorderMode = EffectBorderMode.Hard };
        Glow = new GaussianBlurEffect { Source = Crop, BorderMode = EffectBorderMode.Soft };
    }

    public CropEffect Crop { get; }
    public GaussianBlurEffect Glow { get; }

    public void DisposeEffetcts()
    {
        Crop?.Dispose();
        Glow?.Dispose();
    }
}