using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsLayerConfig : ObservableObject, ICloneable
{
    public LyricsLayerConfig()
    {
    }

    public LyricsLayerConfig(LyricsLayerType lyricsLayerType)
    {
        LyricsLayerType = lyricsLayerType;
    }

    [ObservableProperty] public partial LyricsLayerType LyricsLayerType { get; set; }

    [JsonIgnore] public string LyricsLayerName => LyricsLayerType.ToDisplayName();

    public object Clone()
    {
        return new LyricsLayerConfig
        {
            LyricsLayerType = LyricsLayerType
        };
    }
}