using BetterLyrics.Core.Models.SettingsSchema;

namespace BetterLyrics.Core.Interfaces
{
    public interface IConfigurable
    {
        IEnumerable<SettingDef> GetSettings();

        void OnConfigChanged(Dictionary<string, object> newConfig);
    }
}
