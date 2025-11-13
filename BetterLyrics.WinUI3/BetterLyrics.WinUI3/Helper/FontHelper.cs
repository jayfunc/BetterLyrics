using Microsoft.Graphics.Canvas.Text;
using System.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        public static string[] SystemFontFamilies => CanvasTextFormat.GetSystemFontFamilies().Order().ToArray();
    }
}
