// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Services
{
    public interface ISettingsService
    {
        AppSettings AppSettings { get; set; }

        void UpdateGlobalStyles(bool useCustom);

        bool ImportSettings(string importPath);
        void ExportSettings(string exportPath);
    }
}
