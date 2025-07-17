using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Enums
{
    public enum DockPlacement
    {
        Top,
        Bottom
    }

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
}
