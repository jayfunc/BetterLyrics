using BetterLyrics.Core.Abstractions.Plugins;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

public static class LangGenerator
{
    public static void Run(Type configType, string langsDir, IEnumerable<string> targetLanguages, string defaultLang = "en-US")
    {
        Directory.CreateDirectory(langsDir);
        var codeMap = ExtractFromCode(configType);
        foreach (var lang in targetLanguages)
        {
            string filePath = Path.Combine(langsDir, $"{lang}.json");

            if (lang.Equals(defaultLang, StringComparison.OrdinalIgnoreCase))
            {
                SaveJson(codeMap, filePath);
            }
            else
            {
                SyncTargetLanguage(codeMap, filePath);
            }
        }
    }

    public static void Run<TConfig>(string langsDir, IEnumerable<string> targetLanguages, string defaultLang = "en-US")
        where TConfig : PluginConfigBase
    {
        Run(typeof(TConfig), langsDir, targetLanguages, defaultLang);
    }

    private static void SyncTargetLanguage(SortedDictionary<string, string> codeMap, string filePath)
    {
        var currentMap = ReadJson(filePath);
        var newMap = new SortedDictionary<string, string>();

        foreach (var kvp in codeMap)
        {
            string key = kvp.Key;
            string defaultVal = kvp.Value;

            if (currentMap.TryGetValue(key, out var existingVal) && !string.IsNullOrWhiteSpace(existingVal))
            {
                newMap[key] = existingVal;
            }
            else
            {
                newMap[key] = $"[TODO] {defaultVal}";
            }
        }

        SaveJson(newMap, filePath);
    }

    private static SortedDictionary<string, string> ExtractFromCode(Type type)
    {
        var dict = new SortedDictionary<string, string>();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            if (!prop.CanWrite) continue;

            string baseKey = $"Settings.{prop.Name}";

            var attr = prop.GetCustomAttribute<DisplayAttribute>();
            string label = attr?.Name ?? SplitCamelCase(prop.Name);
            string desc = attr?.Description ?? "";

            dict[$"{baseKey}.Label"] = label;
            dict[$"{baseKey}.Desc"] = desc;

            if (prop.PropertyType.IsEnum)
            {
                foreach (string name in Enum.GetNames(prop.PropertyType))
                {
                    dict[$"{baseKey}.Option.{name}"] = name;
                }
            }
        }
        return dict;

    }

    private static SortedDictionary<string, string> ExtractFromCode<TConfig>()
    {
        return ExtractFromCode(typeof(TConfig));
    }

    private static Dictionary<string, string> ReadJson(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var opts = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, opts) ?? new();
        }
        catch { return new(); }
    }

    private static void SaveJson(object data, string path)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(path, JsonSerializer.Serialize(data, opts));
    }

    private static string SplitCamelCase(string str) =>
        System.Text.RegularExpressions.Regex.Replace(str, "([A-Z])", " $1").Trim();

}
