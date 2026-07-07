using System.Text.Json.Serialization;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsStyleSettings : ObservableRecipient, ICloneable
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsDynamicLyricsFontSize { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PhoneticLyricsFontSize { get; set; } = 12;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int OriginalLyricsFontSize { get; set; } = 24;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int TranslatedLyricsFontSize { get; set; } = 12;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PhoneticLyricsOpacity { get; set; } = 60; // 60 %

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PlayedOriginalLyricsOpacity { get; set; } = 100; // 100 % 已播放

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("OriginalLyricsOpacity")]
    public partial int UnplayedOriginalLyricsOpacity { get; set; } = 30; // 30 % 未播放

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int TranslatedLyricsOpacity { get; set; } = 60; // 60 %

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool UseInternalLyricsAlignment { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsLayoutOrientation LyricsLayoutOrientation { get; set; } = LyricsLayoutOrientation.Horizontal;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<LyricsLayerConfig> LyricsLayerOrder { get; set; } = new()
    {
        new LyricsLayerConfig(LyricsLayerType.Tertiary), new LyricsLayerConfig(LyricsLayerType.Primary),
        new LyricsLayerConfig(LyricsLayerType.Secondary)
    };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool AutoWrap { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int LyricsFontStrokeWidth { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppColor LyricsCustomBgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("LyricsCustomFgFontColor")]
    public partial AppColor LyricsCustomPlayedFgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppColor LyricsCustomUnplayedFgFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("LyricsCustomStrokeFontColor")]
    public partial AppColor LyricsCustomPlayedStrokeFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppColor LyricsCustomUnplayedStrokeFontColor { get; set; } = Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsFontColorType LyricsBgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("LyricsFgFontColorType")]
    public partial LyricsFontColorType LyricsPlayedFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsFontColorType LyricsUnplayedFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("LyricsStrokeFontColorType")]
    public partial LyricsFontColorType LyricsPlayedStrokeFontColorType { get; set; } =
        LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsFontColorType LyricsUnplayedStrokeFontColorType { get; set; } =
        LyricsFontColorType.AdaptiveGrayed;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [JsonPropertyName("LyricsLineSpacingFactor")]
    public partial double LyricsLineOverallSpacingFactor { get; set; } = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double LyricsLineInnerSpacingFactor { get; set; } = 0.1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LyricsCJKFontFamily { get; set; } = "Arial";

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LyricsWesternFontFamily { get; set; } = "Arial";

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PlayingLineTopOffset { get; set; } = 50; // 50 %

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