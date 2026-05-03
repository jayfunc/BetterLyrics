using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Text.Json.Serialization;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsWindowStatus : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsDefault { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MonitorDeviceName { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsWorkArea { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTop { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTopPolling { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsShownInSwitchers { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsWallpaper { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLocked { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsBorderlessWhenLocked { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysHideUnlockButton { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool KeepNowPlayingBarInteractiveWhenLocked { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTimelineLyricsPreviewEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysHidePlayingBar { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPinToTaskbar { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TaskbarPlacement TaskbarPlacement { get; set; } = TaskbarPlacement.Left;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsMaximized { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFullscreen { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Guid LayoutProfileId { get; set; } = Guid.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients][NotifyPropertyChangedFor(nameof(DemoWindowMargin))] public partial Rect WindowBounds { get; set; } = new Rect(100, 100, 800, 500);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double DockHeight { get; set; } = 64;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(DemoWindowMargin))] public partial Rect MonitorBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DockPlacement DockPlacement { get; set; } = DockPlacement.Top;
        [ObservableProperty] public partial LyricsStyleSettings LyricsStyleSettings { get; set; } = new();
        [ObservableProperty] public partial LyricsEffectSettings LyricsEffectSettings { get; set; } = new(500, 500, 500, EasingType.Quad);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsBackgroundSettings LyricsBackgroundSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtAreaStyleSettings AlbumArtLayoutSettings { get; set; } = new();
        [ObservableProperty] public partial AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAdaptToEnvironment { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowPixelSampleMode EnvironmentSampleMode { get; set; } = WindowPixelSampleMode.WindowEdge;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ElementTheme WindowTheme { get; set; } = ElementTheme.Dark;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial PaletteGeneratorType PaletteGeneratorType { get; set; } = PaletteGeneratorType.MedianCut;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial NowPlayingPalette WindowPalette { get; set; } = new();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoShowOrHideWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int AutoShowOrHideWindowDelay { get; set; } = 250; // 250ms
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TitleBarArea TitleBarArea { get; set; } = TitleBarArea.Top;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsKeepScreenOpen { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEdgeFeatheringEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int EdgeFeatheringLeft { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int EdgeFeatheringTop { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int EdgeFeatheringRight { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int EdgeFeatheringBottom { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsCardStyleKey { get; set; } = "LyricsCardStickyNoteStyle";

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpoutOutputEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FPS FPS { get; set; } = FPS.Hz60;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowDebugOverlay { get; set; } = false;

        [JsonIgnore][ObservableProperty] public partial bool IsOverlayInputHelperRunning { get; set; } = false;
        [JsonIgnore][ObservableProperty] public partial bool IsAlwaysOnTopPollingTimerRunning { get; set; } = false;
        [JsonIgnore][ObservableProperty] public partial bool IsUnderlayColorTimerRunning { get; set; } = false;

        [JsonIgnore][ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowStatus WindowStatus { get; set; } = WindowStatus.Closed;
        [JsonIgnore] public DispatcherQueueTimer? VisibilityTimer { get; set; }
        [JsonIgnore] public Thickness DemoWindowMargin => new(WindowBounds.Left - MonitorBounds.Left, WindowBounds.Top - MonitorBounds.Top, 0, 0);

        public LyricsWindowStatus()
        {
            LyricsStyleSettings.PropertyChanged += LyricsStyleSettings_PropertyChanged;
            LyricsEffectSettings.PropertyChanged += LyricsEffectSettings_PropertyChanged;
            LyricsBackgroundSettings.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
            AlbumArtLayoutSettings.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
            AlbumArtAreaEffectSettings.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;

            var primaryMonitorInfoEx = MonitorHook.GetPrimaryMonitorInfoEx();
            var monitorRect = primaryMonitorInfoEx.rcMonitor;

            MonitorDeviceName = primaryMonitorInfoEx.szDevice;
            MonitorBounds = monitorRect.ToRect();
        }

        public LyricsWindowStatus(LyricsWindowMode mode) : this()
        {
            ILocalizationService localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

            switch (mode)
            {
                case LyricsWindowMode.Standard:
                    InitStandardMode(localizationService);
                    break;
                case LyricsWindowMode.Narrow:
                    InitNarrowMode(localizationService);
                    break;
                case LyricsWindowMode.Fullscreen:
                    InitFullscreenMode(localizationService);
                    break;
                case LyricsWindowMode.Desktop:
                    InitDesktopMode(localizationService);
                    break;
                case LyricsWindowMode.Docked:
                    InitDockedMode(localizationService);
                    break;
                case LyricsWindowMode.Taskbar:
                    InitTaskbarMode(localizationService);
                    break;
                case LyricsWindowMode.Wallpaper:
                    InitWallpaperMode(localizationService);
                    break;
                default:
                    break;
            }
        }

        private void InitDesktopMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("DesktopMode");
            IsLocked = true;
            IsAlwaysOnTop = true;
            IsAlwaysOnTopPolling = true;
            IsAdaptToEnvironment = true;
            IsShownInSwitchers = false;
            EnvironmentSampleMode = WindowPixelSampleMode.WindowEdge;
            LyricsStyleSettings = new()
            {
                LyricsAlignmentType = TextAlignmentType.Center
            };
            LyricsBackgroundSettings = new LyricsBackgroundSettings
            {
                IsFluidOverlayEnabled = false
            };
            WindowBounds = MonitorBounds.ToCenterPart(3);
        }

        private void InitDockedMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("DockedMode");
            IsWorkArea = true;
            IsAlwaysOnTop = true;
            IsAlwaysOnTopPolling = true;
            IsAdaptToEnvironment = true;
            IsShownInSwitchers = false;
            EnvironmentSampleMode = WindowPixelSampleMode.BelowWindow;
            IsAlwaysHideUnlockButton = true;
            KeepNowPlayingBarInteractiveWhenLocked = true;
            LyricsStyleSettings = new LyricsStyleSettings
            {
                LyricsAlignmentType = TextAlignmentType.Center
            };
            LyricsBackgroundSettings = new LyricsBackgroundSettings
            {
                IsFluidOverlayEnabled = false,
                IsPureColorOverlayEnabled = true
            };
            WindowBounds = this.GetAppBarBounds();
        }

        private void InitFullscreenMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("FullscreenMode");
            LyricsStyleSettings = new LyricsStyleSettings
            {
                LyricsAlignmentType = TextAlignmentType.Center
            };
            IsFullscreen = true;
            WindowBounds = MonitorBounds;
        }

        private void InitStandardMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("StandardMode");
            WindowBounds = MonitorBounds.ToCenterPart(2);
        }

        private void InitNarrowMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("NarrowMode");
            WindowBounds = MonitorBounds.ToCenterPart(4, 1.5);
        }

        private void InitTaskbarMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("TaskbarMode");
            IsPinToTaskbar = true;
            IsLocked = true;
            IsAdaptToEnvironment = true;
            IsShownInSwitchers = false;
            EnvironmentSampleMode = WindowPixelSampleMode.BelowWindow;
            IsAlwaysHideUnlockButton = true;
            KeepNowPlayingBarInteractiveWhenLocked = true;
            LyricsStyleSettings = new()
            {
                LyricsAlignmentType = TextAlignmentType.Left,
                AutoWrap = false
            };
            LyricsBackgroundSettings = new LyricsBackgroundSettings
            {
                IsFluidOverlayEnabled = false
            };
            WindowBounds = this.GetTaskbarDemoBounds();
        }

        private void InitWallpaperMode(ILocalizationService localizationService)
        {
            Name = localizationService.GetLocalizedString("WallpaperMode");
            IsWallpaper = true;
            IsLocked = true;
            IsAlwaysOnTop = true;
            IsAlwaysOnTopPolling = true;
            IsAdaptToEnvironment = true;
            IsShownInSwitchers = false;
            EnvironmentSampleMode = WindowPixelSampleMode.Wallpaper;
            LyricsStyleSettings = new()
            {
                LyricsAlignmentType = TextAlignmentType.Center
            };
            LyricsBackgroundSettings = new LyricsBackgroundSettings
            {
                IsFluidOverlayEnabled = false
            };
            WindowBounds = MonitorBounds.ToCenterPart(3);
        }

        partial void OnLyricsStyleSettingsChanged(LyricsStyleSettings oldValue, LyricsStyleSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsStyleSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsStyleSettings_PropertyChanged;
        }

        private void LyricsStyleSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsStyleSettings));
        }

        partial void OnLyricsEffectSettingsChanged(LyricsEffectSettings oldValue, LyricsEffectSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsEffectSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsEffectSettings_PropertyChanged;
        }

        private void LyricsEffectSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsEffectSettings));
        }

        partial void OnLyricsBackgroundSettingsChanged(LyricsBackgroundSettings oldValue, LyricsBackgroundSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsBackgroundSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
        }

        private void LyricsBackgroundSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsBackgroundSettings));
        }

        partial void OnAlbumArtLayoutSettingsChanged(AlbumArtAreaStyleSettings oldValue, AlbumArtAreaStyleSettings newValue)
        {
            oldValue.PropertyChanged -= AlbumArtLayoutSettings_PropertyChanged;
            newValue.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
        }

        private void AlbumArtLayoutSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtLayoutSettings));
        }

        partial void OnAlbumArtAreaEffectSettingsChanged(AlbumArtAreaEffectSettings oldValue, AlbumArtAreaEffectSettings newValue)
        {
            oldValue.PropertyChanged -= AlbumArtAreaEffectSettings_PropertyChanged;
            newValue.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;
        }

        private void AlbumArtAreaEffectSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtAreaEffectSettings));
        }

        public object Clone()
        {
            return new LyricsWindowStatus()
            {
                Name = this.Name,
                IsDefault = this.IsDefault,
                MonitorDeviceName = this.MonitorDeviceName,
                IsWorkArea = this.IsWorkArea,
                IsAlwaysOnTop = this.IsAlwaysOnTop,
                IsAlwaysOnTopPolling = this.IsAlwaysOnTopPolling,
                IsShownInSwitchers = this.IsShownInSwitchers,
                IsWallpaper = this.IsWallpaper,
                IsLocked = this.IsLocked,
                IsBorderlessWhenLocked = this.IsBorderlessWhenLocked,
                IsAlwaysHideUnlockButton = this.IsAlwaysHideUnlockButton,

                IsPinToTaskbar = this.IsPinToTaskbar,
                TaskbarPlacement = this.TaskbarPlacement,

                IsMaximized = this.IsMaximized,
                IsFullscreen = this.IsFullscreen,

                LayoutProfileId = this.LayoutProfileId,

                WindowBounds = this.WindowBounds,
                DockHeight = this.DockHeight,
                MonitorBounds = this.MonitorBounds,
                DockPlacement = this.DockPlacement,

                LyricsStyleSettings = (LyricsStyleSettings)this.LyricsStyleSettings.Clone(),
                LyricsEffectSettings = (LyricsEffectSettings)this.LyricsEffectSettings.Clone(),
                LyricsBackgroundSettings = (LyricsBackgroundSettings)this.LyricsBackgroundSettings.Clone(),
                AlbumArtLayoutSettings = (AlbumArtAreaStyleSettings)this.AlbumArtLayoutSettings.Clone(),
                AlbumArtAreaEffectSettings = (AlbumArtAreaEffectSettings)this.AlbumArtAreaEffectSettings.Clone(),

                IsAdaptToEnvironment = this.IsAdaptToEnvironment,
                EnvironmentSampleMode = this.EnvironmentSampleMode,
                WindowTheme = this.WindowTheme,
                PaletteGeneratorType = this.PaletteGeneratorType,
                WindowPalette = this.WindowPalette,

                AutoShowOrHideWindow = this.AutoShowOrHideWindow,
                AutoShowOrHideWindowDelay = this.AutoShowOrHideWindowDelay,
                TitleBarArea = this.TitleBarArea,
                IsKeepScreenOpen = this.IsKeepScreenOpen,

                IsEdgeFeatheringEnabled = this.IsEdgeFeatheringEnabled,
                EdgeFeatheringLeft = this.EdgeFeatheringLeft,
                EdgeFeatheringTop = this.EdgeFeatheringTop,
                EdgeFeatheringRight = this.EdgeFeatheringRight,
                EdgeFeatheringBottom = this.EdgeFeatheringBottom,

                LyricsCardStyleKey = this.LyricsCardStyleKey,

                IsSpoutOutputEnabled = this.IsSpoutOutputEnabled,

                FPS = this.FPS,
                ShowDebugOverlay = this.ShowDebugOverlay,

                IsTimelineLyricsPreviewEnabled = this.IsTimelineLyricsPreviewEnabled,
                KeepNowPlayingBarInteractiveWhenLocked = this.KeepNowPlayingBarInteractiveWhenLocked,
                IsAlwaysHidePlayingBar = this.IsAlwaysHidePlayingBar,
            };

        }
    }
}
