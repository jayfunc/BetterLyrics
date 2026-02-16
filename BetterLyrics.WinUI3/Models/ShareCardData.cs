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
        public Brush? OverlayBrush { get; set; } // 动态提取的背景色
        public List<string> SelectedLyrics { get; set; } = new();
    }
}
