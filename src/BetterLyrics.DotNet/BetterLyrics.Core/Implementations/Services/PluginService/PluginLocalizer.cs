using System.Globalization;
using System.Text.Json;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Sdk.Interfaces.Plugins;

namespace BetterLyrics.Core.Implementations.Services.PluginService;

public class PluginLocalizer : ILocalizer
{
    private readonly string _pluginDir;
    private Dictionary<string, string> _translations = new();

    public PluginLocalizer(string pluginDir)
    {
        _pluginDir = pluginDir;
        LoadTranslations();
    }

    public string CurrentLanguage { get; private set; } = "en";

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        if (_translations.TryGetValue(key, out var value)) return value;
        return key;
    }

    private void LoadTranslations()
    {
        var langFolder = Path.Combine(_pluginDir, "Langs");
        if (!Directory.Exists(langFolder)) return;

        // Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride 
        // 或者 System.Globalization.CultureInfo.CurrentUICulture.Name
        var userLang = CultureInfo.CurrentUICulture.Name;

        var targetFile = Path.Combine(langFolder, $"{userLang}.json");

        if (!File.Exists(targetFile))
        {
            var fallback = Directory.GetFiles(langFolder, $"{userLang.Split('-')[0]}-*.json").FirstOrDefault();
            targetFile = fallback ?? Path.Combine(langFolder, "en.json");
        }

        if (File.Exists(targetFile))
            try
            {
                var json = File.ReadAllText(targetFile);
                var dict = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DictionaryStringString);
                if (dict != null)
                {
                    _translations = dict;
                    CurrentLanguage = Path.GetFileNameWithoutExtension(targetFile);
                }
            }
            catch
            {
                /* 记录日志：翻译文件损坏 */
            }
    }
}