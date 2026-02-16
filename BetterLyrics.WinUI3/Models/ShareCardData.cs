using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsShareCardData
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public ImageSource? CoverImage { get; set; }
        public Brush? OverlayBrush { get; set; }
        public List<LyricsLine> SelectedLyrics { get; set; } = new();
    }
}
