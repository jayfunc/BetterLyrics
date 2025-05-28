using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.Settings {
    public class SettingsService : ISettingsService {
        private readonly ApplicationDataContainer _localSettings;

        public SettingsService() {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public T Get<T>(string key, T defaultValue = default) {
            if (_localSettings.Values.TryGetValue(key, out object value)) {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        public void Set<T>(string key, T value) {
            _localSettings.Values[key] = value;
        }

    }
}
