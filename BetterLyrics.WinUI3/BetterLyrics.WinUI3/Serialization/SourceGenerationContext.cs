// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Serialization
{
    [JsonSerializable(typeof(LibreTranslateResponse))]
    [JsonSerializable(typeof(CutletDockerResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(LyricsSearchResult))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
