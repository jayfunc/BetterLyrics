using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsLayerConfig : ObservableObject, ICloneable
    {
        [ObservableProperty] public partial LyricsLayerType LyricsLayerType { get; set; }

        [JsonIgnore]
        public string LyricsLayerName => LyricsLayerType.ToDisplayName();

        public LyricsLayerConfig() { }

        public LyricsLayerConfig(LyricsLayerType lyricsLayerType)
        {
            LyricsLayerType = lyricsLayerType;
        }

        public object Clone()
        {
            return new LyricsLayerConfig()
            {
                LyricsLayerType = this.LyricsLayerType
            };
        }
    }
}
