using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Extensions;

public static class DockPlacementExtensions
{
    public static WindowPixelSampleMode ToWindowPixelSampleMode(this DockPlacement placement)
    {
        return placement switch
        {
            DockPlacement.Top => WindowPixelSampleMode.BelowWindow,
            DockPlacement.Bottom => WindowPixelSampleMode.AboveWindow,
            _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null)
        };
    }
}