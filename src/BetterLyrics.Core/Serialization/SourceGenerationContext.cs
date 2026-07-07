using System.Text.Json;
using System.Text.Json.Serialization;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Http;
using BetterLyrics.Core.Models.Memory;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Serialization;

[JsonSerializable(typeof(LibreTranslateResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(LyricsCacheItem))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(MemoryReaderConfig))]
[JsonSerializable(typeof(LayoutProfile))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class SourceGenerationContext : JsonSerializerContext
{
}