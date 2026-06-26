using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings
{
    public partial class LyricsEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial WordByWordEffectMode WordByWordEffectMode { get; set; } = WordByWordEffectMode.Auto;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsBlurEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFadeOutEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsEdgeFeatheringEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsOutOfSightEffectEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectScope LyricsGlowEffectScope { get; set; } = LyricsEffectScope.LongDurationSyllable;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsGlowEffectLongSyllableDuration { get; set; } = 700; // 700ms
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsGlowEffectAmount { get; set; } = 8;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsShadowEffectEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsScaleEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScaleEffectLongSyllableDuration { get; set; } = 700; // 700ms
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsScaleEffectAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScaleEffectAmount { get; set; } = 115; // 115%

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationAmountAutoAdjust { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFloatAnimationAmount { get; set; } = 8;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFloatAnimationDuration { get; set; } = 450; // 450ms

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial EasingType LyricsScrollEasingType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial EaseMode LyricsScrollEasingMode { get; set; } = EaseMode.Out;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDelay { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDelay { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFanLyricsEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FanLyricsAngle { get; set; } = 30;

        [ObservableProperty][NotifyPropertyChangedRecipients][NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))] public partial bool Is3DLyricsEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients][NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))] public partial bool IsAuto3DLyricsEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DXAngle { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DYAngle { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DZAngle { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Lyrics3DDepth { get; set; } = 800;
        [JsonIgnore] public bool Is3DLyricsAdjustable => Is3DLyricsEnabled && !IsAuto3DLyricsEnabled;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsBrethingEffectEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsBreathingIntensity { get; set; } = 80;

        public LyricsEffectSettings(int lyricsScrollTopDuration = 500, int lyricsScrollDuration = 500, int lyricsScrollBottomDuration = 500, EasingType lyricsScrollEasingType = EasingType.Quad)
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
                WordByWordEffectMode = this.WordByWordEffectMode,

                IsLyricsBlurEffectEnabled = this.IsLyricsBlurEffectEnabled,
                IsLyricsFadeOutEffectEnabled = this.IsLyricsFadeOutEffectEnabled,
                IsLyricsEdgeFeatheringEffectEnabled = this.IsLyricsEdgeFeatheringEffectEnabled,
                IsLyricsOutOfSightEffectEnabled = this.IsLyricsOutOfSightEffectEnabled,

                IsLyricsGlowEffectEnabled = this.IsLyricsGlowEffectEnabled,
                LyricsGlowEffectLongSyllableDuration = this.LyricsGlowEffectLongSyllableDuration,
                IsLyricsGlowEffectAmountAutoAdjust = this.IsLyricsGlowEffectAmountAutoAdjust,
                LyricsGlowEffectAmount = this.LyricsGlowEffectAmount,
                LyricsGlowEffectScope = this.LyricsGlowEffectScope,

                IsLyricsShadowEffectEnabled = this.IsLyricsShadowEffectEnabled,

                IsLyricsScaleEffectEnabled = this.IsLyricsScaleEffectEnabled,
                LyricsScaleEffectLongSyllableDuration = this.LyricsScaleEffectLongSyllableDuration,
                IsLyricsScaleEffectAmountAutoAdjust = this.IsLyricsScaleEffectAmountAutoAdjust,
                LyricsScaleEffectAmount = this.LyricsScaleEffectAmount,

                IsLyricsFloatAnimationEnabled = this.IsLyricsFloatAnimationEnabled,
                IsLyricsFloatAnimationAmountAutoAdjust = this.IsLyricsFloatAnimationAmountAutoAdjust,
                LyricsFloatAnimationAmount = this.LyricsFloatAnimationAmount,
                LyricsFloatAnimationDuration = this.LyricsFloatAnimationDuration,

                LyricsScrollEasingType = this.LyricsScrollEasingType,
                LyricsScrollEasingMode = this.LyricsScrollEasingMode,
                LyricsScrollDuration = this.LyricsScrollDuration,
                LyricsScrollTopDuration = this.LyricsScrollTopDuration,
                LyricsScrollBottomDuration = this.LyricsScrollBottomDuration,
                LyricsScrollTopDelay = this.LyricsScrollTopDelay,
                LyricsScrollBottomDelay = this.LyricsScrollBottomDelay,

                IsFanLyricsEnabled = this.IsFanLyricsEnabled,
                FanLyricsAngle = this.FanLyricsAngle,

                Is3DLyricsEnabled = this.Is3DLyricsEnabled,
                IsAuto3DLyricsEnabled = this.IsAuto3DLyricsEnabled,
                Lyrics3DXAngle = this.Lyrics3DXAngle,
                Lyrics3DYAngle = this.Lyrics3DYAngle,
                Lyrics3DZAngle = this.Lyrics3DZAngle,
                Lyrics3DDepth = this.Lyrics3DDepth,

                IsLyricsBrethingEffectEnabled = this.IsLyricsBrethingEffectEnabled,
                LyricsBreathingIntensity = this.LyricsBreathingIntensity,
            };
        }
    }
}
