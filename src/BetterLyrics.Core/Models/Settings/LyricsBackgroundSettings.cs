using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings
{
    public partial class LyricsBackgroundSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPureColorOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PureColorOverlayOpacity { get; set; } = 100; // 100 % = 1.0

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsCoverOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlaySpeed { get; set; } = 50;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayBlurAmount { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsCoverOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsCoverOverlayParallaxEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayOpacity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayLightWaveEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsColorDitheringEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayStatic { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayParallaxEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SpectrumPlacement SpectrumPlacement { get; set; } = SpectrumPlacement.Bottom;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SpectrumStyle SpectrumStyle { get; set; } = SpectrumStyle.Curve;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumCount { get; set; } = 32;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumSensitivity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumGlowEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumOpacity { get; set; } = 100; // 100%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType SpectrumColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AppColor SpectrumCustomColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumOverlayParallaxEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlayAmount { get; set; } = 10;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlaySpeed { get; set; } = 1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayParallaxEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FogOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayParallaxEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRaindropOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropSpeed { get; set; } = 100; // 100%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropSize { get; set; } = 100; // 100%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropDensity { get; set; } = 40; // 40%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropLightAngle { get; set; } = 135; // 135 degree
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropShadowIntensity { get; set; } = 0; // 0%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRaindropOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRaindropOverlayParallaxEnabled { get; set; } = false;

        public LyricsBackgroundSettings() { }

        public object Clone()
        {
            return new LyricsBackgroundSettings
            {
                IsPureColorOverlayEnabled = this.IsPureColorOverlayEnabled,
                PureColorOverlayOpacity = this.PureColorOverlayOpacity,

                IsCoverOverlayEnabled = this.IsCoverOverlayEnabled,
                CoverOverlayOpacity = this.CoverOverlayOpacity,
                CoverOverlaySpeed = this.CoverOverlaySpeed,
                CoverOverlayBlurAmount = this.CoverOverlayBlurAmount,
                CoverOverlayBreathingIntensity = this.CoverOverlayBreathingIntensity,
                IsCoverOverlayBrethingEffectEnabled = this.IsCoverOverlayBrethingEffectEnabled,
                IsCoverOverlayParallaxEnabled = this.IsCoverOverlayParallaxEnabled,

                IsFluidOverlayEnabled = this.IsFluidOverlayEnabled,
                FluidOverlayOpacity = this.FluidOverlayOpacity,
                FluidOverlayBreathingIntensity = this.FluidOverlayBreathingIntensity,
                IsFluidOverlayBrethingEffectEnabled = this.IsFluidOverlayBrethingEffectEnabled,
                IsFluidOverlayLightWaveEnabled = this.IsFluidOverlayLightWaveEnabled,
                IsColorDitheringEnabled = this.IsColorDitheringEnabled,
                IsFluidOverlayStatic = this.IsFluidOverlayStatic,
                IsFluidOverlayParallaxEnabled = this.IsFluidOverlayParallaxEnabled,

                IsSpectrumOverlayEnabled = this.IsSpectrumOverlayEnabled,
                SpectrumPlacement = this.SpectrumPlacement,
                SpectrumStyle = this.SpectrumStyle,
                SpectrumCount = this.SpectrumCount,
                SpectrumSensitivity = this.SpectrumSensitivity,
                IsSpectrumGlowEffectEnabled = this.IsSpectrumGlowEffectEnabled,
                IsSpectrumBrethingEffectEnabled = this.IsSpectrumBrethingEffectEnabled,
                SpectrumBreathingIntensity = this.SpectrumBreathingIntensity,
                SpectrumOpacity = this.SpectrumOpacity,
                SpectrumColorType = this.SpectrumColorType,
                SpectrumCustomColor = this.SpectrumCustomColor,
                IsSpectrumOverlayParallaxEnabled = this.IsSpectrumOverlayParallaxEnabled,

                IsSnowFlakeOverlayEnabled = this.IsSnowFlakeOverlayEnabled,
                SnowFlakeOverlayAmount = this.SnowFlakeOverlayAmount,
                SnowFlakeOverlaySpeed = this.SnowFlakeOverlaySpeed,
                SnowFlakeOverlayBreathingIntensity = this.SnowFlakeOverlayBreathingIntensity,
                IsSnowFlakeOverlayBrethingEffectEnabled = this.IsSnowFlakeOverlayBrethingEffectEnabled,
                IsSnowFlakeOverlayParallaxEnabled = this.IsSnowFlakeOverlayParallaxEnabled,

                IsFogOverlayEnabled = this.IsFogOverlayEnabled,
                FogOverlayBreathingIntensity = this.FogOverlayBreathingIntensity,
                IsFogOverlayBrethingEffectEnabled = this.IsFogOverlayBrethingEffectEnabled,
                IsFogOverlayParallaxEnabled = this.IsFogOverlayParallaxEnabled,

                IsRaindropOverlayEnabled = this.IsRaindropOverlayEnabled,
                RaindropSpeed = this.RaindropSpeed,
                RaindropSize = this.RaindropSize,
                RaindropDensity = this.RaindropDensity,
                RaindropLightAngle = this.RaindropLightAngle,
                RaindropShadowIntensity = this.RaindropShadowIntensity,
                RaindropOverlayBreathingIntensity = this.RaindropOverlayBreathingIntensity,
                IsRaindropOverlayBrethingEffectEnabled = this.IsRaindropOverlayBrethingEffectEnabled,
                IsRaindropOverlayParallaxEnabled = this.IsRaindropOverlayParallaxEnabled,
            };
        }
    }
}
