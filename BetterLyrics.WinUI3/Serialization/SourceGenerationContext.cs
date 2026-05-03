// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Http;
using BetterLyrics.WinUI3.Models.Settings;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Serialization
{
    [JsonSerializable(typeof(LibreTranslateResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(LyricsCacheItem))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(MemoryReaderConfig))]
    [JsonSerializable(typeof(LayoutProfile))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
