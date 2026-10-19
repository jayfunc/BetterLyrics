using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsBackgroundSettings : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial bool IsPureColorOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int PureColorOverlayOpacity { get; set; } = 100; // 100 % = 1.0

    [ObservableProperty]
    public partial PureColorOverlayColorType PureColorOverlayColorType { get; set; } = PureColorOverlayColorType.AdaptiveColored;

    [ObservableProperty]
    public partial AppColor PureColorOverlayCustomColor { get; set; } = Colors.White;

    [ObservableProperty]
    public partial bool IsDesktopWallpaperOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int DesktopWallpaperOverlayOpacity { get; set; } = 100; // 100 % = 1.0

    [ObservableProperty]
    public partial int DesktopWallpaperOverlayBlurAmount { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsCoverOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0

    [ObservableProperty]
    public partial int CoverOverlaySpeed { get; set; } = 50;

    [ObservableProperty]
    public partial int CoverOverlayBlurAmount { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsCoverOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int CoverOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial bool IsCoverOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFluidOverlayEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int FluidOverlayOpacity { get; set; } = 100;

    [ObservableProperty]
    public partial int FluidOverlaySpeed { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsFluidOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int FluidOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial bool IsFluidOverlayLightWaveEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsColorDitheringEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFluidOverlayStatic { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFluidOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSpectrumOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial SpectrumPlacement SpectrumPlacement { get; set; } = SpectrumPlacement.Bottom;

    [ObservableProperty]
    public partial SpectrumStyle SpectrumStyle { get; set; } = SpectrumStyle.Curve;

    [ObservableProperty]
    public partial int SpectrumCount { get; set; } = 32;

    [ObservableProperty]
    public partial int SpectrumSensitivity { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsSpectrumGlowEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSpectrumBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int SpectrumBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial int SpectrumOpacity { get; set; } = 100; // 100%

    [ObservableProperty]
    public partial LyricsFontColorType SpectrumColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    public partial AppColor SpectrumCustomColor { get; set; } = Colors.White;

    [ObservableProperty]
    public partial bool IsSpectrumOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int SpectrumDelayMs { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSnowFlakeOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int SnowFlakeOverlayAmount { get; set; } = 10;

    [ObservableProperty]
    public partial int SnowFlakeOverlaySpeed { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsSnowFlakeOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int SnowFlakeOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial bool IsSnowFlakeOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFogOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFogOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int FogOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial bool IsFogOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsRaindropOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int RaindropSpeed { get; set; } = 100; // 100%

    [ObservableProperty]
    public partial int RaindropSize { get; set; } = 100; // 100%

    [ObservableProperty]
    public partial int RaindropDensity { get; set; } = 40; // 40%

    [ObservableProperty]
    public partial int RaindropLightAngle { get; set; } = 135; // 135 degree

    [ObservableProperty]
    public partial int RaindropShadowIntensity { get; set; } = 0; // 0%

    [ObservableProperty]
    public partial bool IsRaindropOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int RaindropOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    public partial bool IsRaindropOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSemanticEffectEnabled { get; set; } = false;

    partial void OnSpectrumColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsBackgroundColorConfigChangedMessage(this));

    partial void OnSpectrumCustomColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsBackgroundColorConfigChangedMessage(this));

    partial void OnSpectrumCountChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new SpectrumConfigChangedMessage(this));

    partial void OnSpectrumSensitivityChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new SpectrumConfigChangedMessage(this));

    partial void OnSpectrumDelayMsChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new SpectrumConfigChangedMessage(this));

    public object Clone()
    {
        return new LyricsBackgroundSettings
        {
            IsPureColorOverlayEnabled = IsPureColorOverlayEnabled,
            PureColorOverlayOpacity = PureColorOverlayOpacity,
            PureColorOverlayColorType = PureColorOverlayColorType,
            PureColorOverlayCustomColor = PureColorOverlayCustomColor,

            IsDesktopWallpaperOverlayEnabled = IsDesktopWallpaperOverlayEnabled,
            DesktopWallpaperOverlayOpacity = DesktopWallpaperOverlayOpacity,
            DesktopWallpaperOverlayBlurAmount = DesktopWallpaperOverlayBlurAmount,

            IsCoverOverlayEnabled = IsCoverOverlayEnabled,
            CoverOverlayOpacity = CoverOverlayOpacity,
            CoverOverlaySpeed = CoverOverlaySpeed,
            CoverOverlayBlurAmount = CoverOverlayBlurAmount,
            CoverOverlayBreathingIntensity = CoverOverlayBreathingIntensity,
            IsCoverOverlayBrethingEffectEnabled = IsCoverOverlayBrethingEffectEnabled,
            IsCoverOverlayParallaxEnabled = IsCoverOverlayParallaxEnabled,

            IsFluidOverlayEnabled = IsFluidOverlayEnabled,
            FluidOverlayOpacity = FluidOverlayOpacity,
            FluidOverlaySpeed = FluidOverlaySpeed,
            FluidOverlayBreathingIntensity = FluidOverlayBreathingIntensity,
            IsFluidOverlayBrethingEffectEnabled = IsFluidOverlayBrethingEffectEnabled,
            IsFluidOverlayLightWaveEnabled = IsFluidOverlayLightWaveEnabled,
            IsColorDitheringEnabled = IsColorDitheringEnabled,
            IsFluidOverlayStatic = IsFluidOverlayStatic,
            IsFluidOverlayParallaxEnabled = IsFluidOverlayParallaxEnabled,

            IsSpectrumOverlayEnabled = IsSpectrumOverlayEnabled,
            SpectrumPlacement = SpectrumPlacement,
            SpectrumStyle = SpectrumStyle,
            SpectrumCount = SpectrumCount,
            SpectrumSensitivity = SpectrumSensitivity,
            IsSpectrumGlowEffectEnabled = IsSpectrumGlowEffectEnabled,
            IsSpectrumBrethingEffectEnabled = IsSpectrumBrethingEffectEnabled,
            SpectrumBreathingIntensity = SpectrumBreathingIntensity,
            SpectrumOpacity = SpectrumOpacity,
            SpectrumColorType = SpectrumColorType,
            SpectrumCustomColor = SpectrumCustomColor,
            IsSpectrumOverlayParallaxEnabled = IsSpectrumOverlayParallaxEnabled,
            SpectrumDelayMs = SpectrumDelayMs,

            IsSnowFlakeOverlayEnabled = IsSnowFlakeOverlayEnabled,
            SnowFlakeOverlayAmount = SnowFlakeOverlayAmount,
            SnowFlakeOverlaySpeed = SnowFlakeOverlaySpeed,
            SnowFlakeOverlayBreathingIntensity = SnowFlakeOverlayBreathingIntensity,
            IsSnowFlakeOverlayBrethingEffectEnabled = IsSnowFlakeOverlayBrethingEffectEnabled,
            IsSnowFlakeOverlayParallaxEnabled = IsSnowFlakeOverlayParallaxEnabled,

            IsFogOverlayEnabled = IsFogOverlayEnabled,
            FogOverlayBreathingIntensity = FogOverlayBreathingIntensity,
            IsFogOverlayBrethingEffectEnabled = IsFogOverlayBrethingEffectEnabled,
            IsFogOverlayParallaxEnabled = IsFogOverlayParallaxEnabled,

            IsRaindropOverlayEnabled = IsRaindropOverlayEnabled,
            RaindropSpeed = RaindropSpeed,
            RaindropSize = RaindropSize,
            RaindropDensity = RaindropDensity,
            RaindropLightAngle = RaindropLightAngle,
            RaindropShadowIntensity = RaindropShadowIntensity,
            RaindropOverlayBreathingIntensity = RaindropOverlayBreathingIntensity,
            IsRaindropOverlayBrethingEffectEnabled = IsRaindropOverlayBrethingEffectEnabled,
            IsRaindropOverlayParallaxEnabled = IsRaindropOverlayParallaxEnabled,
            IsSemanticEffectEnabled = IsSemanticEffectEnabled
        };
    }
}
