using BetterLyrics.Core.Interfaces.Plugins;
using System.Text.Json;

namespace BetterLyrics.Core.Implementations.Services.PluginService
{
    public class PluginLocalizer : ILocalizer
    {
        private Dictionary<string, string> _translations = new();
        private readonly string _pluginDir;

        public string CurrentLanguage { get; private set; } = "en";

        public PluginLocalizer(string pluginDir)
        {
            _pluginDir = pluginDir;
            LoadTranslations();
        }

        public string this[string key] => GetString(key);

        public string GetString(string key)
        {
            if (_translations.TryGetValue(key, out var value))
            {
                return value;
            }
            return key;
        }

        private void LoadTranslations()
        {
            string langFolder = Path.Combine(_pluginDir, "Langs");
            if (!Directory.Exists(langFolder)) return;

            // Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride 
            // 或者 System.Globalization.CultureInfo.CurrentUICulture.Name
            string userLang = System.Globalization.CultureInfo.CurrentUICulture.Name;

            string targetFile = Path.Combine(langFolder, $"{userLang}.json");

            if (!File.Exists(targetFile))
            {
                var fallback = Directory.GetFiles(langFolder, $"{userLang.Split('-')[0]}-*.json").FirstOrDefault();
                targetFile = fallback ?? Path.Combine(langFolder, "en.json");
            }

            if (File.Exists(targetFile))
            {
                try
                {
                    string json = File.ReadAllText(targetFile);
                    var dict = JsonSerializer.Deserialize(json, Core.Serialization.SourceGenerationContext.Default.DictionaryStringString);
                    if (dict != null)
                    {
                        _translations = dict;
                        CurrentLanguage = Path.GetFileNameWithoutExtension(targetFile);
                    }
                }
                catch { /* 记录日志：翻译文件损坏 */ }
            }
        }
    }
}