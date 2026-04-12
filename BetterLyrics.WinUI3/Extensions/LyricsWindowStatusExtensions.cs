using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models.Settings;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsWindowStatusExtensions
    {
        extension(LyricsWindowStatus status)
        {
            public Rect GetAppBarBounds() =>
                status.MonitorBounds
                .WithY(status.DockPlacement switch
                {
                    DockPlacement.Top => status.MonitorBounds.Top,
                    DockPlacement.Bottom => status.MonitorBounds.Bottom - status.DockHeight,
                    _ => status.MonitorBounds.Top,
                })
                .WithHeight(status.DockPlacement switch
                {
                    DockPlacement.Top => status.DockHeight,
                    DockPlacement.Bottom => status.DockHeight,
                    _ => status.DockHeight,
                });

            public Rect GetTaskbarDemoBounds() =>
                status.MonitorBounds
                .WithY(status.MonitorBounds.Bottom - 64)
                .WithHeight(64);
        }
    }
}
