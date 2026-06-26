using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings
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
