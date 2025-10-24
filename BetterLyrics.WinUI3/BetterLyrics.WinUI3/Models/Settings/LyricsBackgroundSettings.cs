using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using BetterLyrics.WinUI3.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsBackgroundSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ElementTheme LyricsBackgroundTheme { get; set; } = ElementTheme.Dark;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPureColorOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PureColorOverlayOpacity { get; set; } = 100; // 100 % = 1.0

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsCoverOverlayEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayBlurAmount { get; set; } = 100; // 100 % of the cover image size
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlayOpacity { get; set; } = 100; // 100 % = 1.0
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverOverlaySpeed { get; set; } = 50; // 50 % of the base rotate speed
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverAcrylicEffectAmount { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFluidOverlayEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FluidOverlayOpacity { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial PaletteGeneratorType PaletteGeneratorType { get; set; } = PaletteGeneratorType.MedianCut;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsSpectrumOverlayEnabled { get; set; } = false;


        public LyricsBackgroundSettings() { }

        public object Clone()
        {
            return new LyricsBackgroundSettings
            {
                LyricsBackgroundTheme = this.LyricsBackgroundTheme,
                CoverOverlayBlurAmount = this.CoverOverlayBlurAmount,
                CoverOverlayOpacity = this.CoverOverlayOpacity,
                PureColorOverlayOpacity = this.PureColorOverlayOpacity,
                CoverOverlaySpeed = this.CoverOverlaySpeed,
                CoverAcrylicEffectAmount = this.CoverAcrylicEffectAmount,
                PaletteGeneratorType = this.PaletteGeneratorType
            };
        }
    }
}
