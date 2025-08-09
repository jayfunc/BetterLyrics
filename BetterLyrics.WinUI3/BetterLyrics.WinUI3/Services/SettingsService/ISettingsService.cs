// 2025/6/23 by Zhe Fang

using System.Collections.Generic;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml;
using Windows.UI;

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
