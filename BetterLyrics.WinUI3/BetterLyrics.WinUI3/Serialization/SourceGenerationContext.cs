// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Serialization
{

    [JsonSerializable(typeof(List<AlbumArtSearchProviderInfo>))]
    [JsonSerializable(typeof(List<LyricsSearchProviderInfo>))]
    [JsonSerializable(typeof(List<MediaSourceProviderInfo>))]
    [JsonSerializable(typeof(List<LocalMediaFolder>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(TranslateResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(double))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
