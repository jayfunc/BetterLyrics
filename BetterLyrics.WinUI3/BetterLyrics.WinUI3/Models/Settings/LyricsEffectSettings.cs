using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsBlurEffectEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectScope LyricsGlowEffectScope { get; set; } = LyricsEffectScope.LongDurationSyllable;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsGlowEffectLongSyllableDuration { get; set; } = 700; // 700ms
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsGlowEffectAmount { get; set; } = 8;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsScaleEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScaleEffectLongSyllableDuration { get; set; } = 700; // 700ms
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsScaleEffectAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScaleEffectAmount { get; set; } = 115; // 115%

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFloatAnimationAmount { get; set; } = 8;

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
                LyricsGlowEffectLongSyllableDuration = this.LyricsGlowEffectLongSyllableDuration,
                IsLyricsGlowEffectAmountAutoAdjust = this.IsLyricsGlowEffectAmountAutoAdjust,
                LyricsGlowEffectAmount = this.LyricsGlowEffectAmount,

                IsLyricsScaleEffectEnabled = this.IsLyricsScaleEffectEnabled,
                LyricsScaleEffectLongSyllableDuration = this.LyricsScaleEffectLongSyllableDuration,
                IsLyricsScaleEffectAmountAutoAdjust = this.IsLyricsScaleEffectAmountAutoAdjust,
                LyricsScaleEffectAmount = this.LyricsScaleEffectAmount,

                IsLyricsFloatAnimationEnabled = this.IsLyricsFloatAnimationEnabled,
                IsLyricsFloatAnimationAmountAutoAdjust = this.IsLyricsFloatAnimationAmountAutoAdjust,
                LyricsFloatAnimationAmount = this.LyricsFloatAnimationAmount,

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
