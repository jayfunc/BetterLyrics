using System.Runtime.CompilerServices;

namespace BetterLyrics.Core.Abstractions
{
    public abstract class PluginConfigBase
    {
        private Dictionary<string, object> _settingsStore;

        public void Bind(Dictionary<string, object> settings)
        {
            _settingsStore = settings;
        }

        protected T Get<T>(T defaultValue = default, [CallerMemberName] string key = null)
        {
            if (_settingsStore != null && _settingsStore.TryGetValue(key, out var val))
            {
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        protected void Set<T>(T value, [CallerMemberName] string key = null)
        {
            if (_settingsStore != null)
            {
                _settingsStore[key] = value;
                // 这里还可以触发一个 OnConfigChanged 事件
            }
        }

    }
}
