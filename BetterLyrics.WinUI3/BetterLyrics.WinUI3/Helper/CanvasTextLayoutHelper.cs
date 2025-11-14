using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Helper
{
    public static class CanvasTextLayoutExtensions
    {
        public static void SetFontFamily(this CanvasTextLayout? layout, string? text, string cjk, string latin)
        {
            if (layout == null) return;
            if (text == null) return;

            for (int i = 0; i < text.Length; i++)
            {
                layout.SetFontFamily(i, 1, LanguageHelper.IsCJK(text[i]) ? cjk : latin);
            }
        }
    }
}
