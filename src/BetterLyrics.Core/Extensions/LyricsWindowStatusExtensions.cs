using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;
using LyricsWindowStatus = BetterLyrics.Core.Models.Settings.LyricsWindowStatus;

namespace BetterLyrics.Core.Extensions
{
    public static class LyricsWindowStatusExtensions
    {
        extension(LyricsWindowStatus status)
        {
            public AppRect GetAppBarBounds() =>
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

            public AppRect GetTaskbarDemoBounds() =>
                status.MonitorBounds
                .WithY(status.MonitorBounds.Bottom - 64)
                .WithHeight(64);

            public NowPlayingLayoutMode GetDefaultLayoutProfileMode()
            {
                if (status.IsPinToTaskbar) return NowPlayingLayoutMode.LeftAlbumArtRightLyricsCompact;
                else if (status.IsWallpaper) return NowPlayingLayoutMode.LyricsOnly;
                else if (status.IsWorkArea) return NowPlayingLayoutMode.LyricsOnly;
                else if (status.IsFullscreen) return NowPlayingLayoutMode.TopAlbumArtBottomLyrics;
                else if (!status.LyricsBackgroundSettings.IsPureColorOverlayEnabled &&
                    !status.LyricsBackgroundSettings.IsCoverOverlayEnabled &&
                    !status.LyricsBackgroundSettings.IsFluidOverlayEnabled) return NowPlayingLayoutMode.LyricsOnly;
                else if (status.WindowBounds.Width > status.WindowBounds.Height) return NowPlayingLayoutMode.LeftAlbumArtRightLyrics;
                else return NowPlayingLayoutMode.TopAlbumArtBottomLyricsCompact;
            }

        }
    }
}
