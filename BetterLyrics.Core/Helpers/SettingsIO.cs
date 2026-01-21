using System.Text.Json.Serialization.Metadata;

namespace BetterLyrics.Core.Helpers
{
    public static class SettingsIO
    {
        public static void SaveSettings<T>(string path, T settings, JsonTypeInfo<T> jsonTypeInfo)
        {
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings, jsonTypeInfo));
        }

        public static T ReadSettings<T>(string path, JsonTypeInfo<T> jsonTypeInfo) where T : new()
        {
            if (!File.Exists(path))
                return new T();

            var json = File.ReadAllText(path);
            var data = System.Text.Json.JsonSerializer.Deserialize(json, jsonTypeInfo);

            if (data == null)
                return new T();

            return data;
        }
    }
}
