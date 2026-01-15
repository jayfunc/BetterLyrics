using BetterLyrics.Core.Interfaces;
using System.Runtime.CompilerServices;

namespace BetterLyrics.Core.Abstractions
{
    public abstract class PluginConfigBase
    {
        private IConfigurator? _configurator;

        public void BindConfigurator(IConfigurator configurator)
        {
            _configurator = configurator;
        }

        protected T Get<T>(T defaultValue = default, [CallerMemberName] string key = null)
        {
            try
            {
                return (T)Convert.ChangeType(_configurator.Get(key, defaultValue), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        protected void Set<T>(T value, [CallerMemberName] string key = null)
        {
            _configurator.Set(key, value, Enums.ConfigChangedBy.Plugin);
        }

    }
}
