using System;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public partial class SettingsService : ISettingsService
    {
        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        // Utils

        public T? Get<T>(string key, T? defaultValue = default)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }
    }
}
