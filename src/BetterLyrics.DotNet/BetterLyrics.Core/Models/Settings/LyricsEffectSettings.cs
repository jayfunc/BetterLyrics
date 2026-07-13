using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsEffectSettings : ObservableRecipient, ICloneable
{
    public LyricsEffectSettings(int lyricsScrollTopDuration = 500, int lyricsScrollDuration = 500,
        int lyricsScrollBottomDuration = 500, EasingType lyricsScrollEasingType = EasingType.Quad)
    {
        LyricsScrollTopDuration = lyricsScrollTopDuration;
        LyricsScrollDuration = lyricsScrollDuration;
        LyricsScrollBottomDuration = lyricsScrollBottomDuration;
        LyricsScrollEasingType = lyricsScrollEasingType;
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial WordByWordEffectMode WordByWordEffectMode { get; set; } = WordByWordEffectMode.Auto;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsBlurEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsFadeOutEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsEdgeFeatheringEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsOutOfSightEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsEffectScope LyricsGlowEffectScope { get; set; } = LyricsEffectScope.LongDurationSyllable;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsGlowEffectLongSyllableDuration { get; set; } = 700; // 700ms

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsGlowEffectAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsGlowEffectAmount { get; set; } = 8;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsShadowEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsScaleEffectEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScaleEffectLongSyllableDuration { get; set; } = 700; // 700ms

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsScaleEffectAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScaleEffectAmount { get; set; } = 115; // 115%

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsFloatAnimationAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsFloatAnimationAmount { get; set; } = 8;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsFloatAnimationDuration { get; set; } = 450; // 450ms

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial EasingType LyricsScrollEasingType { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial EaseMode LyricsScrollEasingMode { get; set; } = EaseMode.Out;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScrollDuration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScrollTopDuration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScrollBottomDuration { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScrollTopDelay { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsScrollBottomDelay { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFanLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int FanLyricsAngle { get; set; } = 30;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))]
    public partial bool Is3DLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))]
    public partial bool IsAuto3DLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Lyrics3DXAngle { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Lyrics3DYAngle { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Lyrics3DZAngle { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Lyrics3DDepth { get; set; } = 800;

    [JsonIgnore] public bool Is3DLyricsAdjustable => Is3DLyricsEnabled && !IsAuto3DLyricsEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLyricsBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsBreathingIntensity { get; set; } = 80;

    public object Clone()
    {
        return new LyricsEffectSettings(LyricsScrollTopDuration, LyricsScrollDuration, LyricsScrollBottomDuration,
            LyricsScrollEasingType)
        {
            WordByWordEffectMode = WordByWordEffectMode,

            IsLyricsBlurEffectEnabled = IsLyricsBlurEffectEnabled,
            IsLyricsFadeOutEffectEnabled = IsLyricsFadeOutEffectEnabled,
            IsLyricsEdgeFeatheringEffectEnabled = IsLyricsEdgeFeatheringEffectEnabled,
            IsLyricsOutOfSightEffectEnabled = IsLyricsOutOfSightEffectEnabled,

            IsLyricsGlowEffectEnabled = IsLyricsGlowEffectEnabled,
            LyricsGlowEffectLongSyllableDuration = LyricsGlowEffectLongSyllableDuration,
            IsLyricsGlowEffectAmountAutoAdjust = IsLyricsGlowEffectAmountAutoAdjust,
            LyricsGlowEffectAmount = LyricsGlowEffectAmount,
            LyricsGlowEffectScope = LyricsGlowEffectScope,

            IsLyricsShadowEffectEnabled = IsLyricsShadowEffectEnabled,

            IsLyricsScaleEffectEnabled = IsLyricsScaleEffectEnabled,
            LyricsScaleEffectLongSyllableDuration = LyricsScaleEffectLongSyllableDuration,
            IsLyricsScaleEffectAmountAutoAdjust = IsLyricsScaleEffectAmountAutoAdjust,
            LyricsScaleEffectAmount = LyricsScaleEffectAmount,

            IsLyricsFloatAnimationEnabled = IsLyricsFloatAnimationEnabled,
            IsLyricsFloatAnimationAmountAutoAdjust = IsLyricsFloatAnimationAmountAutoAdjust,
            LyricsFloatAnimationAmount = LyricsFloatAnimationAmount,
            LyricsFloatAnimationDuration = LyricsFloatAnimationDuration,

            LyricsScrollEasingType = LyricsScrollEasingType,
            LyricsScrollEasingMode = LyricsScrollEasingMode,
            LyricsScrollDuration = LyricsScrollDuration,
            LyricsScrollTopDuration = LyricsScrollTopDuration,
            LyricsScrollBottomDuration = LyricsScrollBottomDuration,
            LyricsScrollTopDelay = LyricsScrollTopDelay,
            LyricsScrollBottomDelay = LyricsScrollBottomDelay,

            IsFanLyricsEnabled = IsFanLyricsEnabled,
            FanLyricsAngle = FanLyricsAngle,

            Is3DLyricsEnabled = Is3DLyricsEnabled,
            IsAuto3DLyricsEnabled = IsAuto3DLyricsEnabled,
            Lyrics3DXAngle = Lyrics3DXAngle,
            Lyrics3DYAngle = Lyrics3DYAngle,
            Lyrics3DZAngle = Lyrics3DZAngle,
            Lyrics3DDepth = Lyrics3DDepth,

            IsLyricsBrethingEffectEnabled = IsLyricsBrethingEffectEnabled,
            LyricsBreathingIntensity = LyricsBreathingIntensity
        };
    }
}