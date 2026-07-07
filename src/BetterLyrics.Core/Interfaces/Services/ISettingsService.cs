// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ISettingsService
{
    AppSettings AppSettings { get; set; }

    bool ImportSettings(string importPath);
    void ExportSettings(string exportPath);
}