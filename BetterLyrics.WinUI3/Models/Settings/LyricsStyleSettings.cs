using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System;
using System.Text.Json.Serialization;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsStyleSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsDynamicLyricsFontSize { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PhoneticLyricsFontSize { get; set; } = 12;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int OriginalLyricsFontSize { get; set; } = 24;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int TranslatedLyricsFontSize { get; set; } = 12;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PhoneticLyricsOpacity { get; set; } = 60; // 60 %
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PlayedOriginalLyricsOpacity { get; set; } = 100; // 100 % 已播放
        [ObservableProperty][NotifyPropertyChangedRecipients][JsonPropertyName("OriginalLyricsOpacity")] public partial int UnplayedOriginalLyricsOpacity { get; set; } = 30; // 30 % 未播放
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int TranslatedLyricsOpacity { get; set; } = 60; // 60 %

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsLineContentOrientation LyricsLineContentOrientation { get; set; } = LyricsLineContentOrientation.Vertical;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoWrap { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFontStrokeWidth { get; set; } = 0;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomBgFontColor { get; set; } = Colors.White;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [JsonPropertyName("LyricsCustomFgFontColor")]
        public partial Color LyricsCustomPlayedFgFontColor { get; set; } = Colors.White;
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomUnplayedFgFontColor { get; set; } = Colors.White;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [JsonPropertyName("LyricsCustomStrokeFontColor")]
        public partial Color LyricsCustomPlayedStrokeFontColor { get; set; } = Colors.White;
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomUnplayedStrokeFontColor { get; set; } = Colors.White;

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
        public partial LyricsFontColorType LyricsPlayedStrokeFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsUnplayedStrokeFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        [JsonPropertyName("LyricsLineSpacingFactor")]
        public partial double LyricsLineOverallSpacingFactor { get; set; } = 0.5;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsLineInnerSpacingFactor { get; set; } = 0.1;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsCJKFontFamily { get; set; } = "Arial";
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsWesternFontFamily { get; set; } = "Arial";

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PlayingLineTopOffset { get; set; } = 50; // 50 %

        public LyricsStyleSettings() { }

        public object Clone()
        {
            return new LyricsStyleSettings
            {
                IsDynamicLyricsFontSize = this.IsDynamicLyricsFontSize,
                PhoneticLyricsFontSize = this.PhoneticLyricsFontSize,
                OriginalLyricsFontSize = this.OriginalLyricsFontSize,
                TranslatedLyricsFontSize = this.TranslatedLyricsFontSize,

                PhoneticLyricsOpacity = this.PhoneticLyricsOpacity,
                PlayedOriginalLyricsOpacity = this.PlayedOriginalLyricsOpacity,
                UnplayedOriginalLyricsOpacity = this.UnplayedOriginalLyricsOpacity,
                TranslatedLyricsOpacity = this.TranslatedLyricsOpacity,

                LyricsAlignmentType = this.LyricsAlignmentType,
                LyricsLineContentOrientation = this.LyricsLineContentOrientation,
                AutoWrap = this.AutoWrap,
                LyricsFontStrokeWidth = this.LyricsFontStrokeWidth,
                LyricsCustomBgFontColor = this.LyricsCustomBgFontColor,
                LyricsCustomPlayedFgFontColor = this.LyricsCustomPlayedFgFontColor,
                LyricsCustomUnplayedFgFontColor = this.LyricsCustomUnplayedFgFontColor,
                LyricsCustomPlayedStrokeFontColor = this.LyricsCustomPlayedStrokeFontColor,
                LyricsCustomUnplayedStrokeFontColor = this.LyricsCustomUnplayedStrokeFontColor,
                LyricsBgFontColorType = this.LyricsBgFontColorType,
                LyricsPlayedFgFontColorType = this.LyricsPlayedFgFontColorType,
                LyricsUnplayedFgFontColorType = this.LyricsUnplayedFgFontColorType,
                LyricsPlayedStrokeFontColorType = this.LyricsPlayedStrokeFontColorType,
                LyricsUnplayedStrokeFontColorType = this.LyricsUnplayedStrokeFontColorType,
                LyricsFontWeight = this.LyricsFontWeight,
                LyricsLineOverallSpacingFactor = this.LyricsLineOverallSpacingFactor,
                LyricsCJKFontFamily = this.LyricsCJKFontFamily,
                LyricsWesternFontFamily = this.LyricsWesternFontFamily,

                PlayingLineTopOffset = this.PlayingLineTopOffset,
            };
        }
    }
}
