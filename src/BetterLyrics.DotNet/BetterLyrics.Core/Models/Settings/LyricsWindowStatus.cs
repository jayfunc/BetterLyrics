using System.ComponentModel;
using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsWindowStatus : ObservableRecipient, ICloneable
{
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();

    public LyricsWindowStatus()
    {
        LyricsStyleSettings.PropertyChanged += LyricsStyleSettings_PropertyChanged;
        LyricsEffectSettings.PropertyChanged += LyricsEffectSettings_PropertyChanged;
        LyricsBackgroundSettings.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
        AlbumArtLayoutSettings.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
        AlbumArtAreaEffectSettings.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;

        (MonitorDeviceName, MonitorBounds) = _monitorProvider.GetPrimaryMonitorInfo();
    }

    public LyricsWindowStatus(LyricsWindowMode mode) : this()
    {
        var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

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
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsDefault { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string MonitorDeviceName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsWorkArea { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsAlwaysOnTop { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsAlwaysOnTopPolling { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsShownInSwitchers { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsWallpaper { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLocked { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsBorderlessWhenLocked { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsAlwaysHideUnlockButton { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool KeepNowPlayingBarInteractiveWhenLocked { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsTimelineLyricsPreviewEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsAlwaysHidePlayingBar { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsPinToTaskbar { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TaskbarPlacement TaskbarPlacement { get; set; } = TaskbarPlacement.Left;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsMaximized { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFullscreen { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial Guid LayoutProfileId { get; set; } = Guid.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(DemoWindowMargin))]
    public partial AppRect WindowBounds { get; set; } = new(100, 100, 800, 500);

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double DockHeight { get; set; } = 64;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DemoWindowMargin))]
    public partial AppRect MonitorBounds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial DockPlacement DockPlacement { get; set; } = DockPlacement.Top;

    [ObservableProperty] public partial LyricsStyleSettings LyricsStyleSettings { get; set; } = new();
    [ObservableProperty] public partial LyricsEffectSettings LyricsEffectSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsBackgroundSettings LyricsBackgroundSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AlbumArtAreaStyleSettings AlbumArtLayoutSettings { get; set; } = new();

    [ObservableProperty] public partial AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsAdaptToAlbumArtAdjustable))]
    [NotifyPropertyChangedFor(nameof(IsWindowThemeAdjustable))]
    public partial bool IsAdaptToEnvironment { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsAdaptToEnvironmentAdjustable))]
    [NotifyPropertyChangedFor(nameof(IsWindowThemeAdjustable))]
    public partial bool IsAdaptToAlbumArt { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial WindowPixelSampleMode EnvironmentSampleMode { get; set; } = WindowPixelSampleMode.WindowEdge;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppTheme WindowTheme { get; set; } = AppTheme.Dark;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PaletteGeneratorType PaletteGeneratorType { get; set; } = PaletteGeneratorType.CelebiQuantizer;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double PaletteChromaWeight { get; set; } = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double PaletteToneWeight { get; set; } = -0.75;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double PalettePopulationWeight { get; set; } = 3.0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial NowPlayingPalette WindowPalette { get; set; } = new();

    [JsonPropertyName("AutoShowOrHideWindow")]
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool HideWindowWhenPaused { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool HideWindowWhenNullSession { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int AutoShowOrHideWindowDelay { get; set; } = 250; // 250ms

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TitleBarArea TitleBarArea { get; set; } = TitleBarArea.Top;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsKeepScreenOpen { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsEdgeFeatheringEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int EdgeFeatheringLeft { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int EdgeFeatheringTop { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int EdgeFeatheringRight { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int EdgeFeatheringBottom { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LyricsCardStyleKey { get; set; } = "LyricsCardStickyNoteStyle";

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSpoutOutputEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FPS FPS { get; set; } = FPS.Hz60;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowDebugOverlay { get; set; } = false;

    [JsonIgnore] [ObservableProperty] public partial bool IsOverlayInputHelperRunning { get; set; } = false;
    [JsonIgnore] [ObservableProperty] public partial bool IsAlwaysOnTopPollingTimerRunning { get; set; } = false;
    [JsonIgnore] [ObservableProperty] public partial bool IsUnderlayColorTimerRunning { get; set; } = false;

    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial WindowStatus WindowStatus { get; set; } = WindowStatus.Closed;

    [JsonIgnore]
    public AppThickness DemoWindowMargin =>
        new(WindowBounds.Left - MonitorBounds.Left, WindowBounds.Top - MonitorBounds.Top, 0, 0);

    [JsonIgnore] public bool IsWindowThemeAdjustable => !IsAdaptToEnvironment && !IsAdaptToAlbumArt;
    [JsonIgnore] public bool IsAdaptToEnvironmentAdjustable => !IsAdaptToAlbumArt;
    [JsonIgnore] public bool IsAdaptToAlbumArtAdjustable => !IsAdaptToEnvironment;

    public object Clone()
    {
        return new LyricsWindowStatus
        {
            Name = Name,
            IsDefault = IsDefault,
            MonitorDeviceName = MonitorDeviceName,
            IsWorkArea = IsWorkArea,
            IsAlwaysOnTop = IsAlwaysOnTop,
            IsAlwaysOnTopPolling = IsAlwaysOnTopPolling,
            IsShownInSwitchers = IsShownInSwitchers,
            IsWallpaper = IsWallpaper,
            IsLocked = IsLocked,
            IsBorderlessWhenLocked = IsBorderlessWhenLocked,
            IsAlwaysHideUnlockButton = IsAlwaysHideUnlockButton,

            IsPinToTaskbar = IsPinToTaskbar,
            TaskbarPlacement = TaskbarPlacement,

            IsMaximized = IsMaximized,
            IsFullscreen = IsFullscreen,

            LayoutProfileId = LayoutProfileId,

            WindowBounds = WindowBounds,
            DockHeight = DockHeight,
            MonitorBounds = MonitorBounds,
            DockPlacement = DockPlacement,

            LyricsStyleSettings = (LyricsStyleSettings)LyricsStyleSettings.Clone(),
            LyricsEffectSettings = (LyricsEffectSettings)LyricsEffectSettings.Clone(),
            LyricsBackgroundSettings = (LyricsBackgroundSettings)LyricsBackgroundSettings.Clone(),
            AlbumArtLayoutSettings = (AlbumArtAreaStyleSettings)AlbumArtLayoutSettings.Clone(),
            AlbumArtAreaEffectSettings = (AlbumArtAreaEffectSettings)AlbumArtAreaEffectSettings.Clone(),

            IsAdaptToAlbumArt = IsAdaptToAlbumArt,
            IsAdaptToEnvironment = IsAdaptToEnvironment,
            EnvironmentSampleMode = EnvironmentSampleMode,
            WindowTheme = WindowTheme,
            PaletteGeneratorType = PaletteGeneratorType,
            PaletteChromaWeight = PaletteChromaWeight,
            PaletteToneWeight = PaletteToneWeight,
            PalettePopulationWeight = PalettePopulationWeight,
            WindowPalette = WindowPalette,

            HideWindowWhenPaused = HideWindowWhenPaused,
            HideWindowWhenNullSession = HideWindowWhenNullSession,
            AutoShowOrHideWindowDelay = AutoShowOrHideWindowDelay,
            TitleBarArea = TitleBarArea,
            IsKeepScreenOpen = IsKeepScreenOpen,

            IsEdgeFeatheringEnabled = IsEdgeFeatheringEnabled,
            EdgeFeatheringLeft = EdgeFeatheringLeft,
            EdgeFeatheringTop = EdgeFeatheringTop,
            EdgeFeatheringRight = EdgeFeatheringRight,
            EdgeFeatheringBottom = EdgeFeatheringBottom,

            LyricsCardStyleKey = LyricsCardStyleKey,

            IsSpoutOutputEnabled = IsSpoutOutputEnabled,

            FPS = FPS,
            ShowDebugOverlay = ShowDebugOverlay,

            IsTimelineLyricsPreviewEnabled = IsTimelineLyricsPreviewEnabled,
            KeepNowPlayingBarInteractiveWhenLocked = KeepNowPlayingBarInteractiveWhenLocked,
            IsAlwaysHidePlayingBar = IsAlwaysHidePlayingBar
        };
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
        LyricsStyleSettings = new LyricsStyleSettings
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
        LyricsEffectSettings = new LyricsEffectSettings
        {
            IsLyricsEdgeFeatheringEffectEnabled = false
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
        LyricsStyleSettings = new LyricsStyleSettings
        {
            LyricsAlignmentType = TextAlignmentType.Left,
            AutoWrap = false
        };
        LyricsEffectSettings = new LyricsEffectSettings
        {
            IsLyricsEdgeFeatheringEffectEnabled = false
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
        LyricsStyleSettings = new LyricsStyleSettings
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

    private void LyricsStyleSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LyricsStyleSettings));
    }

    partial void OnLyricsEffectSettingsChanged(LyricsEffectSettings oldValue, LyricsEffectSettings newValue)
    {
        oldValue.PropertyChanged -= LyricsEffectSettings_PropertyChanged;
        newValue.PropertyChanged += LyricsEffectSettings_PropertyChanged;
    }

    private void LyricsEffectSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LyricsEffectSettings));
    }

    partial void OnLyricsBackgroundSettingsChanged(LyricsBackgroundSettings oldValue, LyricsBackgroundSettings newValue)
    {
        oldValue.PropertyChanged -= LyricsBackgroundSettings_PropertyChanged;
        newValue.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
    }

    private void LyricsBackgroundSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LyricsBackgroundSettings));
    }

    partial void OnAlbumArtLayoutSettingsChanged(AlbumArtAreaStyleSettings oldValue, AlbumArtAreaStyleSettings newValue)
    {
        oldValue.PropertyChanged -= AlbumArtLayoutSettings_PropertyChanged;
        newValue.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
    }

    private void AlbumArtLayoutSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AlbumArtLayoutSettings));
    }

    partial void OnAlbumArtAreaEffectSettingsChanged(AlbumArtAreaEffectSettings oldValue,
        AlbumArtAreaEffectSettings newValue)
    {
        oldValue.PropertyChanged -= AlbumArtAreaEffectSettings_PropertyChanged;
        newValue.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;
    }

    private void AlbumArtAreaEffectSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AlbumArtAreaEffectSettings));
    }
}