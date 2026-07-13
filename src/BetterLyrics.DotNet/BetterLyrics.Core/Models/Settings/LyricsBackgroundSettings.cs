using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsBackgroundSettings : ObservableRecipient, ICloneable
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsPureColorOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PureColorOverlayOpacity { get; set; } = 100; // 100 % = 1.0

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsCoverOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int CoverOverlaySpeed { get; set; } = 50;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int CoverOverlayBlurAmount { get; set; } = 100;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsCoverOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int CoverOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsCoverOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFluidOverlayEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int FluidOverlayOpacity { get; set; } = 100;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFluidOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int FluidOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFluidOverlayLightWaveEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsColorDitheringEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFluidOverlayStatic { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFluidOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSpectrumOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial SpectrumPlacement SpectrumPlacement { get; set; } = SpectrumPlacement.Bottom;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial SpectrumStyle SpectrumStyle { get; set; } = SpectrumStyle.Curve;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SpectrumCount { get; set; } = 32;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SpectrumSensitivity { get; set; } = 100;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSpectrumGlowEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSpectrumBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SpectrumBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SpectrumOpacity { get; set; } = 100; // 100%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsFontColorType SpectrumColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppColor SpectrumCustomColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSpectrumOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSnowFlakeOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SnowFlakeOverlayAmount { get; set; } = 10;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SnowFlakeOverlaySpeed { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSnowFlakeOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int SnowFlakeOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsSnowFlakeOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFogOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFogOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int FogOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFogOverlayParallaxEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsRaindropOverlayEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropSpeed { get; set; } = 100; // 100%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropSize { get; set; } = 100; // 100%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropDensity { get; set; } = 40; // 40%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropLightAngle { get; set; } = 135; // 135 degree

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropShadowIntensity { get; set; } = 0; // 0%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsRaindropOverlayBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RaindropOverlayBreathingIntensity { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsRaindropOverlayParallaxEnabled { get; set; } = false;

    public object Clone()
    {
        return new LyricsBackgroundSettings
        {
            IsPureColorOverlayEnabled = IsPureColorOverlayEnabled,
            PureColorOverlayOpacity = PureColorOverlayOpacity,

            IsCoverOverlayEnabled = IsCoverOverlayEnabled,
            CoverOverlayOpacity = CoverOverlayOpacity,
            CoverOverlaySpeed = CoverOverlaySpeed,
            CoverOverlayBlurAmount = CoverOverlayBlurAmount,
            CoverOverlayBreathingIntensity = CoverOverlayBreathingIntensity,
            IsCoverOverlayBrethingEffectEnabled = IsCoverOverlayBrethingEffectEnabled,
            IsCoverOverlayParallaxEnabled = IsCoverOverlayParallaxEnabled,

            IsFluidOverlayEnabled = IsFluidOverlayEnabled,
            FluidOverlayOpacity = FluidOverlayOpacity,
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
            IsRaindropOverlayParallaxEnabled = IsRaindropOverlayParallaxEnabled
        };
    }
}