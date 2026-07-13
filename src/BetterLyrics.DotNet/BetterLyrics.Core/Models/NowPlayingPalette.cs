using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Models;

public struct NowPlayingPalette
{
    public AppColor SpectrumColor { get; set; }

    public AppColor NonCurrentLineFillColor { get; set; }

    public AppColor PlayedCurrentLineFillColor { get; set; }
    public AppColor UnplayedCurrentLineFillColor { get; set; }

    public AppColor PlayedTextStrokeColor { get; set; }
    public AppColor UnplayedTextStrokeColor { get; set; }

    public AppColor UnderlayColor { get; set; }

    public AppColor AccentColor1 { get; set; }
    public AppColor AccentColor2 { get; set; }
    public AppColor AccentColor3 { get; set; }
    public AppColor AccentColor4 { get; set; }

    public AppTheme ThemeType { get; set; }
}