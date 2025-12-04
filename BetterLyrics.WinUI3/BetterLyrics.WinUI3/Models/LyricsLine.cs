// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        public List<LyricsSyllable> LyricsSyllables { get; set; } = [];

        public int? DurationMs => EndMs - StartMs;
        public int? EndMs { get; set; }
        public int StartMs { get; set; }

        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText { get; set; } = "";
        /// <summary>
        /// 译文
        /// </summary>
        public string TranslatedText { get; set; } = "";
        /// <summary>
        /// 注音
        /// </summary>
        public string PhoneticText { get; set; } = "";

    }
}
