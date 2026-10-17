using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.LyricsWindow;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsEffectSettings : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial WordByWordEffectMode WordByWordEffectMode { get; set; } = WordByWordEffectMode.Auto;

    [ObservableProperty]
    public partial bool IsLyricsBlurEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLyricsFadeOutEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLyricsEdgeFeatheringEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLyricsOutOfSightEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial LyricsEffectScope LyricsGlowEffectScope { get; set; } = LyricsEffectScope.LongDurationSyllable;

    [ObservableProperty]
    public partial int LyricsGlowEffectLongSyllableDuration { get; set; } = 700; // 700ms

    [ObservableProperty]
    public partial bool IsLyricsGlowEffectAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    public partial int LyricsGlowEffectAmount { get; set; } = 8;

    [ObservableProperty]
    public partial bool IsLyricsShadowEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsLyricsScaleEffectEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int LyricsScaleEffectLongSyllableDuration { get; set; } = 700; // 700ms

    [ObservableProperty]
    public partial bool IsLyricsScaleEffectAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    public partial int LyricsScaleEffectAmount { get; set; } = 115; // 115%

    [ObservableProperty]
    public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLyricsFloatAnimationAmountAutoAdjust { get; set; } = true;

    [ObservableProperty]
    public partial int LyricsFloatAnimationAmount { get; set; } = 8;

    [ObservableProperty]
    public partial int LyricsFloatAnimationDuration { get; set; } = 450; // 450ms

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLyricsScrollEasingModeEnabled))]
    public partial EasingType LyricsScrollEasingType { get; set; } = EasingType.Spring;

    public bool IsLyricsScrollEasingModeEnabled => LyricsScrollEasingType != EasingType.Spring;

    [ObservableProperty]
    public partial EaseMode LyricsScrollEasingMode { get; set; } = EaseMode.Out;

    [ObservableProperty]
    public partial int LyricsScrollDuration { get; set; } = 500;

    [ObservableProperty]
    public partial int LyricsScrollTopDuration { get; set; } = 500;

    [ObservableProperty]
    public partial int LyricsScrollBottomDuration { get; set; } = 500;

    [ObservableProperty]
    public partial int LyricsScrollTopDelay { get; set; } = 0;

    [ObservableProperty]
    public partial int LyricsScrollBottomDelay { get; set; } = 50;

    [ObservableProperty]
    public partial bool IsFanLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int FanLyricsAngle { get; set; } = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))]
    public partial bool Is3DLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3DLyricsAdjustable))]
    public partial bool IsAuto3DLyricsEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int Lyrics3DXAngle { get; set; } = 0;

    [ObservableProperty]
    public partial int Lyrics3DYAngle { get; set; } = 0;

    [ObservableProperty]
    public partial int Lyrics3DZAngle { get; set; } = 0;

    [ObservableProperty]
    public partial int Lyrics3DDepth { get; set; } = 800;

    [JsonIgnore] public bool Is3DLyricsAdjustable => Is3DLyricsEnabled && !IsAuto3DLyricsEnabled;

    [ObservableProperty]
    public partial bool IsLyricsBrethingEffectEnabled { get; set; } = false;

    [ObservableProperty]
    public partial int LyricsBreathingIntensity { get; set; } = 80;

    partial void OnIsFanLyricsEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnIsLyricsBlurEffectEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnIsLyricsFadeOutEffectEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnIsLyricsOutOfSightEffectEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyricsScrollDurationChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyricsScrollTopDurationChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyricsScrollBottomDurationChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyricsScrollTopDelayChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyricsScrollBottomDelayChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnFanLyricsAngleChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyrics3DXAngleChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyrics3DYAngleChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyrics3DZAngleChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnLyrics3DDepthChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectRelayoutChangedMessage(this));

    partial void OnIsLyricsScaleEffectEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectReloadChangedMessage(this));

    partial void OnIsLyricsGlowEffectEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectReloadChangedMessage(this));

    partial void OnLyricsScaleEffectLongSyllableDurationChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectReloadChangedMessage(this));

    partial void OnLyricsGlowEffectLongSyllableDurationChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsEffectReloadChangedMessage(this));

    public object Clone()
    {
        return new LyricsEffectSettings()
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