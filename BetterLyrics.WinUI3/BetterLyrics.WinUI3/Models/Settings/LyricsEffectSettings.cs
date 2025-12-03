using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsBlurEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsScaleEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial EasingType LyricsScrollEasingType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDelay { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDelay { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFanLyricsEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FanLyricsAngle { get; set; } = 30;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool Is3DLyricsEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DXAngle { get; set; } = 30;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DYAngle { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DZAngle { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DDepth { get; set; } = 1000;

        public LyricsEffectSettings(int lyricsScrollTopDuration, int lyricsScrollDuration, int lyricsScrollBottomDuration, EasingType lyricsScrollEasingType)
        {
            LyricsScrollTopDuration = lyricsScrollTopDuration;
            LyricsScrollDuration = lyricsScrollDuration;
            LyricsScrollBottomDuration = lyricsScrollBottomDuration;
            LyricsScrollEasingType = lyricsScrollEasingType;
        }

        public object Clone()
        {
            return new LyricsEffectSettings(this.LyricsScrollTopDuration, this.LyricsScrollDuration, this.LyricsScrollBottomDuration, this.LyricsScrollEasingType)
            {
                IsLyricsBlurEffectEnabled = this.IsLyricsBlurEffectEnabled,
                IsLyricsGlowEffectEnabled = this.IsLyricsGlowEffectEnabled,
                IsLyricsScaleEffectEnabled = this.IsLyricsScaleEffectEnabled,
                IsLyricsFloatAnimationEnabled = this.IsLyricsFloatAnimationEnabled,

                LyricsScrollEasingType = this.LyricsScrollEasingType,
                LyricsScrollDuration = this.LyricsScrollDuration,
                LyricsScrollTopDuration = this.LyricsScrollTopDuration,
                LyricsScrollBottomDuration = this.LyricsScrollBottomDuration,
                LyricsScrollTopDelay = this.LyricsScrollTopDelay,
                LyricsScrollBottomDelay = this.LyricsScrollBottomDelay,

                IsFanLyricsEnabled = this.IsFanLyricsEnabled,
                FanLyricsAngle = this.FanLyricsAngle,

                Is3DLyricsEnabled = this.Is3DLyricsEnabled,
                Lyrics3DXAngle = this.Lyrics3DXAngle,
                Lyrics3DYAngle = this.Lyrics3DYAngle,
                Lyrics3DZAngle = this.Lyrics3DZAngle,
                Lyrics3DDepth = this.Lyrics3DDepth,
            };
        }
    }
}
