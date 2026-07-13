using BetterLyrics.Core.Helpers;
using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Extensions;

public static class CanvasTextLayoutExtensions
{
    extension(CanvasTextLayout? canvasTextLayout)
    {
        public void SetFontFamily(string? text, string cjk, string latin)
        {
            if (canvasTextLayout == null) return;
            if (text == null) return;

            for (var i = 0; i < text.Length; i++)
                canvasTextLayout.SetFontFamily(i, 1, LanguageHelper.IsCJK(text[i]) ? cjk : latin);
        }
    }
}