using BetterLyrics.Core.Abstractions.Plugins;
using System.Reflection;

namespace BetterLyrics.DevTools.Generators
{
    public static class LangResourceGenerator
    {
        private static string[] _supportedLangs = { "ar", "de", "en", "es", "fr", "hi", "id", "ja", "ko", "ms", "pt", "ru", "th", "vi", "zh-Hans", "zh-Hant" };

        public static void Run(Assembly assembly, string pluginDir)
        {
            Console.WriteLine("[Lang] Starting language resource generation...");

            Type? configType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(PluginConfigBase).IsAssignableFrom(t) && !t.IsAbstract);

            if (configType == null)
            {
                Console.WriteLine("   No Config class found (inheriting from PluginConfigBase). Skipping.");
                return;
            }

            Console.WriteLine($"   Found Config: {configType.Name}");

            string langsDir = Path.Combine(pluginDir, "Langs");

            try
            {
                LangGenerator.Run(configType, langsDir, _supportedLangs, defaultLang: "en");
                Console.WriteLine($"   Generated {_supportedLangs.Length} language files in 'Langs/'.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"   Lang Generation Failed: {ex.Message}");
                throw;
            }
        }
    }
}