// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models.Settings;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    public interface ISettingsService
    {
        AppSettings AppSettings { get; set; }
        // App behavior

        bool ImportSettings(string importPath);
        void ExportSettings(string exportPath);
    }
}
