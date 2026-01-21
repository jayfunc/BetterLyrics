using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Interfaces
{
    public interface IConfigurator
    {
        object Get(string key, object defaultValue);
        void Set(string key, object value, ConfigChangedBy configChangedBy);

        event EventHandler<string, ConfigChangedBy>? OnConfigChanged;
    }
}
