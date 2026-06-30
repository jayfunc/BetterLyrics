using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Models
{
    public struct NowPlayingPalette
    {
        public AppColor SpectrumColor;

        public AppColor NonCurrentLineFillColor;

        public AppColor PlayedCurrentLineFillColor;
        public AppColor UnplayedCurrentLineFillColor;

        public AppColor PlayedTextStrokeColor;
        public AppColor UnplayedTextStrokeColor;

        public AppColor UnderlayColor;

        public AppColor AccentColor1;
        public AppColor AccentColor2;
        public AppColor AccentColor3;
        public AppColor AccentColor4;

        public AppTheme ThemeType;
    }
}
