using Microsoft.UI.Xaml;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public struct NowPlayingPalette
    {
        public Color SpectrumColor;

        public Color NonCurrentLineFillColor;

        public Color PlayedCurrentLineFillColor;
        public Color UnplayedCurrentLineFillColor;

        public Color PlayedTextStrokeColor;
        public Color UnplayedTextStrokeColor;

        public Color UnderlayColor;

        public Color AccentColor1;
        public Color AccentColor2;
        public Color AccentColor3;
        public Color AccentColor4;

        public ElementTheme ThemeType;
    }
}
