using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsCardData : ObservableObject
    {
        [ObservableProperty] public partial string Title { get; set; } = "";
        [ObservableProperty] public partial string Artist { get; set; } = "";

        [ObservableProperty] public partial ImageSource? CoverImage { get; set; }
        [ObservableProperty] public partial Brush? OverlayBrush { get; set; }

        [ObservableProperty] public partial List<LyricsLine> SelectedLyrics { get; set; } = new();

        [ObservableProperty] public partial LyricsCardConfig Config { get; set; } = new();

        public string DateLong => DateTime.Now.ToString("dddd, MMMM d");
        public string DateShort => DateTime.Now.ToString("yyyy.MM.dd");

        public string TimeShort => DateTime.Now.ToString("HH:mm");
        public string TimeWithSeconds => DateTime.Now.ToString("HH:mm:ss");
        public string TimeWithSecondsReply => DateTime.Now.AddSeconds(2).ToString("HH:mm:ss");
    }
}
