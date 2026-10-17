using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsStyleSettings : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial bool IsDynamicLyricsFontSize { get; set; } = true;

    [ObservableProperty]
    public partial int PhoneticLyricsFontSize { get; set; } = 12;

    [ObservableProperty]
    public partial int OriginalLyricsFontSize { get; set; } = 24;

    [ObservableProperty]
    public partial int TranslatedLyricsFontSize { get; set; } = 12;

    [ObservableProperty]
    public partial int PhoneticLyricsOpacity { get; set; } = 60; // 60 %

    [ObservableProperty]
    public partial int PlayedOriginalLyricsOpacity { get; set; } = 100; // 100 % 已播

    [ObservableProperty]
    [JsonPropertyName("OriginalLyricsOpacity")]
    public partial int UnplayedOriginalLyricsOpacity { get; set; } = 30; // 30 % 未播

    [ObservableProperty]
    public partial int TranslatedLyricsOpacity { get; set; } = 60; // 60 %

    [ObservableProperty]
    public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;

    [ObservableProperty]
    public partial bool UseInternalLyricsAlignment { get; set; } = true;

    [ObservableProperty]
    public partial LyricsLayoutOrientation LyricsLayoutOrientation { get; set; } = LyricsLayoutOrientation.Horizontal;

    [ObservableProperty]
    public partial FullyObservableCollection<LyricsLayerConfig> LyricsLayerOrder { get; set; } = new()
    {
        new LyricsLayerConfig(LyricsLayerType.Tertiary), new LyricsLayerConfig(LyricsLayerType.Primary),
        new LyricsLayerConfig(LyricsLayerType.Secondary)
    };

    [ObservableProperty]
    public partial bool AutoWrap { get; set; } = true;

    [ObservableProperty]
    public partial int LyricsFontStrokeWidth { get; set; } = 0;

    [ObservableProperty]
    public partial AppColor LyricsCustomBgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [JsonPropertyName("LyricsCustomFgFontColor")]
    public partial AppColor LyricsCustomPlayedFgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    public partial AppColor LyricsCustomUnplayedFgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [JsonPropertyName("LyricsCustomStrokeFontColor")]
    public partial AppColor LyricsCustomPlayedStrokeFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    public partial AppColor LyricsCustomUnplayedStrokeFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    public partial LyricsFontColorType LyricsBgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [JsonPropertyName("LyricsFgFontColorType")]
    public partial LyricsFontColorType LyricsPlayedFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    public partial LyricsFontColorType LyricsUnplayedFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [JsonPropertyName("LyricsStrokeFontColorType")]
    public partial LyricsFontColorType LyricsPlayedStrokeFontColorType { get; set; } =
        LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    public partial LyricsFontColorType LyricsUnplayedStrokeFontColorType { get; set; } =
        LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;

    [ObservableProperty]
    [JsonPropertyName("LyricsLineSpacingFactor")]
    public partial double LyricsLineOverallSpacingFactor { get; set; } = 0.5;

    [ObservableProperty]
    public partial double LyricsLineInnerSpacingFactor { get; set; } = 0.1;

    [ObservableProperty]
    public partial string LyricsCJKFontFamily { get; set; } = "Arial";

    [ObservableProperty]
    public partial string LyricsWesternFontFamily { get; set; } = "Arial";

    [ObservableProperty]
    public partial int PlayingLineTopOffset { get; set; } = 50; // 50 %

    partial void OnLyricsCJKFontFamilyChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new LyricsFontFamilyChangedMessage(this));

    partial void OnLyricsWesternFontFamilyChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new LyricsFontFamilyChangedMessage(this));

    partial void OnLyricsCustomBgFontColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsCustomPlayedFgFontColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsCustomUnplayedFgFontColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsCustomPlayedStrokeFontColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsCustomUnplayedStrokeFontColorChanged(AppColor value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsBgFontColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsPlayedFgFontColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsUnplayedFgFontColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsPlayedStrokeFontColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnLyricsUnplayedStrokeFontColorTypeChanged(LyricsFontColorType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleColorConfigChangedMessage(this));

    partial void OnIsDynamicLyricsFontSizeChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnAutoWrapChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnUseInternalLyricsAlignmentChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsLineOverallSpacingFactorChanged(double value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsLineInnerSpacingFactorChanged(double value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnPhoneticLyricsFontSizeChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnOriginalLyricsFontSizeChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnTranslatedLyricsFontSizeChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsFontStrokeWidthChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnPlayingLineTopOffsetChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnPhoneticLyricsOpacityChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnUnplayedOriginalLyricsOpacityChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnTranslatedLyricsOpacityChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsFontWeightChanged(LyricsFontWeight value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsLayoutOrientationChanged(LyricsLayoutOrientation value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    partial void OnLyricsAlignmentTypeChanged(TextAlignmentType value) =>
        WeakReferenceMessenger.Default.Send(new LyricsStyleRelayoutChangedMessage(this));

    public object Clone()
    {
        return new LyricsStyleSettings
        {
            IsDynamicLyricsFontSize = IsDynamicLyricsFontSize,
            PhoneticLyricsFontSize = PhoneticLyricsFontSize,
            OriginalLyricsFontSize = OriginalLyricsFontSize,
            TranslatedLyricsFontSize = TranslatedLyricsFontSize,

            PhoneticLyricsOpacity = PhoneticLyricsOpacity,
            PlayedOriginalLyricsOpacity = PlayedOriginalLyricsOpacity,
            UnplayedOriginalLyricsOpacity = UnplayedOriginalLyricsOpacity,
            TranslatedLyricsOpacity = TranslatedLyricsOpacity,

            LyricsAlignmentType = LyricsAlignmentType,
            UseInternalLyricsAlignment = UseInternalLyricsAlignment,
            LyricsLayoutOrientation = LyricsLayoutOrientation,
            LyricsLayerOrder =
                new FullyObservableCollection<LyricsLayerConfig>(
                    LyricsLayerOrder.Select(p => (LyricsLayerConfig)p.Clone())),

            AutoWrap = AutoWrap,
            LyricsFontStrokeWidth = LyricsFontStrokeWidth,
            LyricsCustomBgFontColor = LyricsCustomBgFontColor,
            LyricsCustomPlayedFgFontColor = LyricsCustomPlayedFgFontColor,
            LyricsCustomUnplayedFgFontColor = LyricsCustomUnplayedFgFontColor,
            LyricsCustomPlayedStrokeFontColor = LyricsCustomPlayedStrokeFontColor,
            LyricsCustomUnplayedStrokeFontColor = LyricsCustomUnplayedStrokeFontColor,
            LyricsBgFontColorType = LyricsBgFontColorType,
            LyricsPlayedFgFontColorType = LyricsPlayedFgFontColorType,
            LyricsUnplayedFgFontColorType = LyricsUnplayedFgFontColorType,
            LyricsPlayedStrokeFontColorType = LyricsPlayedStrokeFontColorType,
            LyricsUnplayedStrokeFontColorType = LyricsUnplayedStrokeFontColorType,
            LyricsFontWeight = LyricsFontWeight,
            LyricsLineOverallSpacingFactor = LyricsLineOverallSpacingFactor,
            LyricsLineInnerSpacingFactor = LyricsLineInnerSpacingFactor,
            LyricsCJKFontFamily = LyricsCJKFontFamily,
            LyricsWesternFontFamily = LyricsWesternFontFamily,

            PlayingLineTopOffset = PlayingLineTopOffset
        };
    }
}
