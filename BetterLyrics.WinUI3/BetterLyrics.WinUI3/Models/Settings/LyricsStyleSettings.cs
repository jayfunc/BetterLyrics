using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System;
using System.Linq;
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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int OriginalLyricsOpacity { get; set; } = 30; // 30 %
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int TranslatedLyricsOpacity { get; set; } = 60; // 60 %

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFontStrokeWidth { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomBgFontColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomFgFontColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomStrokeFontColor { get; set; } = Colors.White;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsBgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsStrokeFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsLineSpacingFactor { get; set; } = 0.5;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsCJKFontFamily { get; set; } = FontHelper.SystemFontFamilies.FirstOrDefault() ?? "";
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsWesternFontFamily { get; set; } = FontHelper.SystemFontFamilies.FirstOrDefault() ?? "";

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
                OriginalLyricsOpacity = this.OriginalLyricsOpacity,
                TranslatedLyricsOpacity = this.TranslatedLyricsOpacity,

                LyricsAlignmentType = this.LyricsAlignmentType,
                LyricsFontStrokeWidth = this.LyricsFontStrokeWidth,
                LyricsCustomBgFontColor = this.LyricsCustomBgFontColor,
                LyricsCustomFgFontColor = this.LyricsCustomFgFontColor,
                LyricsCustomStrokeFontColor = this.LyricsCustomStrokeFontColor,
                LyricsBgFontColorType = this.LyricsBgFontColorType,
                LyricsFgFontColorType = this.LyricsFgFontColorType,
                LyricsStrokeFontColorType = this.LyricsStrokeFontColorType,
                LyricsFontWeight = this.LyricsFontWeight,
                LyricsLineSpacingFactor = this.LyricsLineSpacingFactor,
                LyricsCJKFontFamily = this.LyricsCJKFontFamily,
                LyricsWesternFontFamily = this.LyricsWesternFontFamily,

                PlayingLineTopOffset = this.PlayingLineTopOffset,
            };
        }
    }
}
