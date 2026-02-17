using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Serialization
{
    [JsonSerializable(typeof(JsonElement))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    public partial class SourceGenerationContext : JsonSerializerContext { }
}
