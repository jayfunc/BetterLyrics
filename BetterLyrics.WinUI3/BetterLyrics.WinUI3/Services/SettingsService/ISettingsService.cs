// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models.Settings;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    public interface ISettingsService
    {
        AppSettings AppSettings { get; set; }

        void UpdateLanguage();
        bool ImportSettings(string importPath);
        void ExportSettings(string exportPath);
    }
}
