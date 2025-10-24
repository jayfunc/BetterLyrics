using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsStyleSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsDynamicLyricsFontSize { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFontSize { get; set; } = 24;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType LyricsAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsBgFontOpacity { get; set; } = 30; // 30% opacity
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFontStrokeWidth { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomBgFontColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomFgFontColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Color LyricsCustomStrokeFontColor { get; set; } = Colors.White;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsBgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsFgFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontColorType LyricsStrokeFontColorType { get; set; } = LyricsFontColorType.AdaptiveGrayed;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsFontWeight LyricsFontWeight { get; set; } = LyricsFontWeight.Bold;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double LyricsLineSpacingFactor { get; set; } = 0.5;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsTranslationSeparator { get; set; } = StringHelper.NewLine;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsFontFamily { get; set; } = FontHelper.SystemFontFamilies.FirstOrDefault() ?? "";

        public LyricsStyleSettings() { }

        public LyricsStyleSettings(int lyricsFontSize, TextAlignmentType lyricsAlignmentType, int lyricsFontStrokeWidth)
        {
            LyricsFontSize = lyricsFontSize;
            LyricsAlignmentType = lyricsAlignmentType;
            LyricsFontStrokeWidth = lyricsFontStrokeWidth;
        }

        public object Clone()
        {
            return new LyricsStyleSettings
            {
                IsDynamicLyricsFontSize = this.IsDynamicLyricsFontSize,
                LyricsFontSize = this.LyricsFontSize,
                LyricsAlignmentType = this.LyricsAlignmentType,
                LyricsBgFontOpacity = this.LyricsBgFontOpacity,
                LyricsFontStrokeWidth = this.LyricsFontStrokeWidth,
                LyricsCustomBgFontColor = this.LyricsCustomBgFontColor,
                LyricsCustomFgFontColor = this.LyricsCustomFgFontColor,
                LyricsCustomStrokeFontColor = this.LyricsCustomStrokeFontColor,
                LyricsBgFontColorType = this.LyricsBgFontColorType,
                LyricsFgFontColorType = this.LyricsFgFontColorType,
                LyricsStrokeFontColorType = this.LyricsStrokeFontColorType,
                LyricsFontWeight = this.LyricsFontWeight,
                LyricsLineSpacingFactor = this.LyricsLineSpacingFactor,
                LyricsTranslationSeparator = this.LyricsTranslationSeparator,
                LyricsFontFamily = this.LyricsFontFamily
            };
        }
    }
}
