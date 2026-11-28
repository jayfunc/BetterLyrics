using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class LyricsOverlayLayerConfig : ObservableObject, ICloneable
{
    public LyricsOverlayLayerConfig()
    {
    }

    public LyricsOverlayLayerConfig(LyricsOverlayLayerType layerType)
    {
        LayerType = layerType;
    }

    [ObservableProperty] public partial LyricsOverlayLayerType LayerType { get; set; }

    [JsonIgnore]
    public bool IsContentAnchor => LayerType is LyricsOverlayLayerType.Lyrics
        or LyricsOverlayLayerType.AlbumArt
        or LyricsOverlayLayerType.SongInfo;

    [JsonIgnore] public string LayerName => LayerType.ToDisplayName();

    public object Clone()
    {
        return new LyricsOverlayLayerConfig
        {
            LayerType = LayerType
        };
    }
}