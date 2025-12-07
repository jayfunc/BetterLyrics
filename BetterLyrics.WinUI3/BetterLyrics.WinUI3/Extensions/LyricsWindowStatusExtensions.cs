using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsWindowStatusExtensions
    {
        private static readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public static LyricsWindowStatus DesktopMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _resourceService.GetLocalizedString("DesktopMode"),
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                WindowBounds = new Rect(100, 100, 600, 250),
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsLocked = true,
                IsAdaptToEnvironment = true,
                IsShownInSwitchers = false,
                EnvironmentSampleMode = WindowPixelSampleMode.WindowEdge,
                LyricsStyleSettings = new()
                {
                    LyricsAlignmentType = TextAlignmentType.Center,
                },
                LyricsBackgroundSettings = new LyricsBackgroundSettings
                {
                    IsFluidOverlayEnabled = false,
                }
            };
        }

        public static LyricsWindowStatus DockedMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            var status = new LyricsWindowStatus(window)
            {
                Name = _resourceService.GetLocalizedString("DockedMode"),
                IsWorkArea = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsAdaptToEnvironment = true,
                IsShownInSwitchers = false,
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                EnvironmentSampleMode = WindowPixelSampleMode.BelowWindow,
                TitleBarArea = TitleBarArea.None,
                LyricsStyleSettings = new LyricsStyleSettings
                {
                    LyricsAlignmentType = TextAlignmentType.Center,
                },
                LyricsBackgroundSettings = new LyricsBackgroundSettings
                {
                    IsFluidOverlayEnabled = false,
                    IsPureColorOverlayEnabled = true,
                }
            };
            status.WindowBounds = status.GetWindowBoundsWhenWorkArea();
            return status;
        }

        public static LyricsWindowStatus FullscreenMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            var status = new LyricsWindowStatus(window)
            {
                Name = _resourceService.GetLocalizedString("FullscreenMode"),
                IsAlwaysOnTop = true,
                TitleBarArea = TitleBarArea.None,
                LyricsLayoutOrientation = LyricsLayoutOrientation.Vertical,
                LyricsStyleSettings = new LyricsStyleSettings
                {
                    LyricsAlignmentType = TextAlignmentType.Center,
                },
                IsFullscreen = true,
            };
            status.WindowBounds = new Rect(
                status.MonitorBounds.X,
                status.MonitorBounds.Y - 1,
                status.MonitorBounds.Width,
                status.MonitorBounds.Height + 1
            );
            return status;
        }

        public static LyricsWindowStatus StandardMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _resourceService.GetLocalizedString("StandardMode"),
            };
        }

        public static LyricsWindowStatus NarrowMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _resourceService.GetLocalizedString("NarrowMode"),
                WindowBounds = new Rect(100, 100, 400, 800),
                LyricsLayoutOrientation = LyricsLayoutOrientation.Vertical,
            };
        }
    }
}
