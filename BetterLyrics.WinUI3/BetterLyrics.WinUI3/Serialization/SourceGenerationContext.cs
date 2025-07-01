// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Serialization
{

    [JsonSerializable(typeof(List<LyricsSearchProviderInfo>))]
    [JsonSerializable(typeof(List<LocalLyricsFolder>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
