using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsWindowStatusExtensions
    {
        private static readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public static LyricsWindowStatus DesktopMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _localizationService.GetLocalizedString("DesktopMode"),
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                WindowBounds = new Rect(100, 100, 600, 250),
                IsLocked = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
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
                Name = _localizationService.GetLocalizedString("DockedMode"),
                IsWorkArea = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsAdaptToEnvironment = true,
                IsShownInSwitchers = false,
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                EnvironmentSampleMode = WindowPixelSampleMode.BelowWindow,
                IsAlwaysHideUnlockButton = true,
                KeepNowPlayingBarInteractiveWhenLocked = true,
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
                Name = _localizationService.GetLocalizedString("FullscreenMode"),
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
                Name = _localizationService.GetLocalizedString("StandardMode"),
            };
        }

        public static LyricsWindowStatus NarrowMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _localizationService.GetLocalizedString("NarrowMode"),
                WindowBounds = new Rect(100, 100, 400, 800),
                LyricsLayoutOrientation = LyricsLayoutOrientation.Vertical,
            };
        }

        public static LyricsWindowStatus TaskbarMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _localizationService.GetLocalizedString("TaskbarMode"),
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                IsPinToTaskbar = true,
                IsLocked = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsAdaptToEnvironment = true,
                IsShownInSwitchers = false,
                EnvironmentSampleMode = WindowPixelSampleMode.WindowEdge,
                IsAlwaysHideUnlockButton = true,
                KeepNowPlayingBarInteractiveWhenLocked = true,
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

        public static LyricsWindowStatus WallpaperMode(Window? window = null)
        {
            window ??= WindowHook.GetWindow<SystemTrayWindow>();
            return new LyricsWindowStatus(window)
            {
                Name = _localizationService.GetLocalizedString("WallpaperMode"),
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                WindowBounds = new Rect(100, 100, 600, 250),
                IsWallpaper = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
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

    }
}
