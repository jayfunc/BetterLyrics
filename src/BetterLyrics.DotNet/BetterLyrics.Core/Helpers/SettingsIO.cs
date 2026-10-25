using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BetterLyrics.Core.Helpers;

public static class SettingsIO
{
    private static readonly object _ioLock = new();

    public static void SaveSettings<T>(string path, T settings, JsonTypeInfo<T> jsonTypeInfo)
    {
        lock (_ioLock)
        {
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            var bakPath = path + ".bak";
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(fileStream, settings, jsonTypeInfo);
                }

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, bakPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempPath, path, overwrite: true);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
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

    public static bool ReadWriteDebugLogs(string path)
    {
        if (TryReadWriteDebugLogs(path, out var enabled))
        {
            return enabled;
        }

        var bakPath = path + ".bak";
        return TryReadWriteDebugLogs(bakPath, out enabled) && enabled;
    }

    private static bool TryReadWriteDebugLogs(string path, out bool enabled)
    {
        enabled = false;
        if (!File.Exists(path)) return false;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;

            if (!document.RootElement.TryGetProperty("AdvancedSettings", out var advancedSettings) ||
                advancedSettings.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!advancedSettings.TryGetProperty("WriteDebugLogs", out var writeDebugLogs))
            {
                return false;
            }

            if (writeDebugLogs.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            enabled = writeDebugLogs.GetBoolean();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadFile<T>(string path, JsonTypeInfo<T> jsonTypeInfo, out T result) where T : new()
    {
        result = new T();
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) || json.StartsWith('\0'))
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