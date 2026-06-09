using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Settings
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

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayOpacity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayBreathingIntensity { get; set; } = 80;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayLightWaveEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsColorDitheringEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayStatic { get; set; } = false;

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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color SpectrumCustomColor { get; set; } = Colors.White;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlayAmount { get; set; } = 10;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlaySpeed { get; set; } = 1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlayBreathingIntensity { get; set; } = 80;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FogOverlayBreathingIntensity { get; set; } = 80;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRaindropOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropSpeed { get; set; } = 100; // 100%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropSize { get; set; } = 100; // 100%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropDensity { get; set; } = 40; // 40%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropLightAngle { get; set; } = 135; // 135 degree
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropShadowIntensity { get; set; } = 0; // 0%
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsRaindropOverlayBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RaindropOverlayBreathingIntensity { get; set; } = 80;

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

                IsFluidOverlayEnabled = this.IsFluidOverlayEnabled,
                FluidOverlayOpacity = this.FluidOverlayOpacity,
                FluidOverlayBreathingIntensity = this.FluidOverlayBreathingIntensity,
                IsFluidOverlayBrethingEffectEnabled = this.IsFluidOverlayBrethingEffectEnabled,
                IsFluidOverlayLightWaveEnabled = this.IsFluidOverlayLightWaveEnabled,
                IsColorDitheringEnabled = this.IsColorDitheringEnabled,

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

                IsSnowFlakeOverlayEnabled = this.IsSnowFlakeOverlayEnabled,
                SnowFlakeOverlayAmount = this.SnowFlakeOverlayAmount,
                SnowFlakeOverlaySpeed = this.SnowFlakeOverlaySpeed,
                SnowFlakeOverlayBreathingIntensity = this.SnowFlakeOverlayBreathingIntensity,
                IsSnowFlakeOverlayBrethingEffectEnabled = this.IsSnowFlakeOverlayBrethingEffectEnabled,

                IsFogOverlayEnabled = this.IsFogOverlayEnabled,
                FogOverlayBreathingIntensity = this.FogOverlayBreathingIntensity,
                IsFogOverlayBrethingEffectEnabled = this.IsFogOverlayBrethingEffectEnabled,

                IsRaindropOverlayEnabled = this.IsRaindropOverlayEnabled,
                RaindropSpeed = this.RaindropSpeed,
                RaindropSize = this.RaindropSize,
                RaindropDensity = this.RaindropDensity,
                RaindropLightAngle = this.RaindropLightAngle,
                RaindropShadowIntensity = this.RaindropShadowIntensity,
                RaindropOverlayBreathingIntensity = this.RaindropOverlayBreathingIntensity,
                IsRaindropOverlayBrethingEffectEnabled = this.IsRaindropOverlayBrethingEffectEnabled,
            };
        }
    }
}
