using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BetterLyrics.Core.Helpers;

public static class SettingsIO
{
    public static void SaveSettings<T>(string path, T settings, JsonTypeInfo<T> jsonTypeInfo)
    {
        var tempPath = path + ".tmp";
        var bakPath = path + ".bak";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, jsonTypeInfo));

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, bakPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, path, overwrite: true);
        }
    }

    public static T ReadSettings<T>(string path, JsonTypeInfo<T> jsonTypeInfo) where T : new()
    {
        if (TryReadFile(path, jsonTypeInfo, out T data))
        {
            return data;
        }

        // Try reading from backup if primary file fails or doesn't exist
        var bakPath = path + ".bak";
        if (File.Exists(bakPath) && TryReadFile(bakPath, jsonTypeInfo, out data))
        {
            return data;
        }

        // Both primary and backup failed or missing
        return new T();
    }

    private static bool TryReadFile<T>(string path, JsonTypeInfo<T> jsonTypeInfo, out T result) where T : new()
    {
        result = new T();
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) || json.StartsWith("\0"))
                return false;

            var data = JsonSerializer.Deserialize(json, jsonTypeInfo);
            if (data != null)
            {
                result = data;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}