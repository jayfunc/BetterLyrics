using System.Collections.Generic;
using System.Numerics;

namespace Impressionist.Abstractions
{
    public class PaletteResult
    {
        public List<Vector3> Palette { get; } = new List<Vector3>();
        public bool PaletteIsDark { get; }
        public ThemeColorResult ThemeColor { get; }
        internal PaletteResult(List<Vector3> palette, bool paletteIsDark, ThemeColorResult themeColor)
        {
            Palette = palette;
            PaletteIsDark = paletteIsDark;
            ThemeColor = themeColor;
        }
    }
    public class ThemeColorResult
    {
        public Vector3 Color { get; }
        public bool ColorIsDark { get; }
        internal ThemeColorResult(Vector3 color, bool colorIsDark)
        {
            Color = color;
            ColorIsDark = colorIsDark;
        }
    }
}
