using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Models.SettingsSchema;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using static Lyricify.Lyrics.Providers.Web.Musixmatch.GetTranslationsResponse;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginConfigurator : IConfigurator
    {
        private readonly string _pluginCfgDir;
        private readonly string _pluginCfgPath;
        private Dictionary<string, object> _config = new();

        public event EventHandler<string, ConfigChangedBy>? OnConfigChanged;

        public PluginConfigurator(string pluginDir)
        {
            _pluginCfgDir = $"{pluginDir}/config";
            _pluginCfgPath = $"{_pluginCfgDir}/config.json";
            EnsureConfigDir();
            ReadSettings();
        }

        public object Get(string key, object defaultValue)
        {
            if (_config.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        public void Set(string key, object value, ConfigChangedBy configChangedBy)
        {
            _config[key] = value;
            SaveSettings();
            OnConfigChanged?.Invoke(key, configChangedBy);
        }

        private void EnsureConfigDir()
        {
            if (!Directory.Exists(_pluginCfgDir))
            {
                Directory.CreateDirectory(_pluginCfgDir);
            }
        }

        private void SaveSettings()
        {
            Core.Helpers.SettingsIO.SaveSettings(_pluginCfgPath, _config, Serialization.SourceGenerationContext.Default.DictionaryStringObject);
        }

        private void ReadSettings()
        {
            _config = Core.Helpers.SettingsIO.ReadSettings(_pluginCfgPath, Serialization.SourceGenerationContext.Default.DictionaryStringObject);
        }
    }
}
