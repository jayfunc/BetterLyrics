using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsBackgroundSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ElementTheme LyricsBackgroundTheme { get; set; } = ElementTheme.Dark;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPureColorOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PureColorOverlayOpacity { get; set; } = 100; // 100 % = 1.0

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsCoverOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlaySpeed { get; set; } = 50;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayBlurAmount { get; set; } = 100;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayOpacity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial PaletteGeneratorType PaletteGeneratorType { get; set; } = PaletteGeneratorType.MedianCut;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SpectrumPlacement SpectrumPlacement { get; set; } = SpectrumPlacement.Bottom;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial SpectrumStyle SpectrumStyle { get; set; } = SpectrumStyle.Bar;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumCount { get; set; } = 128;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SpectrumSensitivity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumGlowEffectEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSnowFlakeOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlayAmount { get; set; } = 10;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SnowFlakeOverlaySpeed { get; set; } = 1;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFogOverlayEnabled { get; set; } = false;


        public LyricsBackgroundSettings() { }

        public object Clone()
        {
            return new LyricsBackgroundSettings
            {
                LyricsBackgroundTheme = this.LyricsBackgroundTheme,

                IsPureColorOverlayEnabled = this.IsPureColorOverlayEnabled,
                PureColorOverlayOpacity = this.PureColorOverlayOpacity,

                IsCoverOverlayEnabled = this.IsCoverOverlayEnabled,
                CoverOverlayOpacity = this.CoverOverlayOpacity,
                CoverOverlaySpeed = this.CoverOverlaySpeed,
                CoverOverlayBlurAmount = this.CoverOverlayBlurAmount,

                IsFluidOverlayEnabled = this.IsFluidOverlayEnabled,
                FluidOverlayOpacity = this.FluidOverlayOpacity,
                PaletteGeneratorType = this.PaletteGeneratorType,

                IsSpectrumOverlayEnabled = this.IsSpectrumOverlayEnabled,
                SpectrumPlacement = this.SpectrumPlacement,
                SpectrumStyle = this.SpectrumStyle,
                SpectrumCount = this.SpectrumCount,

                IsSnowFlakeOverlayEnabled = this.IsSnowFlakeOverlayEnabled,
                SnowFlakeOverlayAmount = this.SnowFlakeOverlayAmount,
                SnowFlakeOverlaySpeed = this.SnowFlakeOverlaySpeed,

                IsFogOverlayEnabled = this.IsFogOverlayEnabled,
            };
        }
    }
}
