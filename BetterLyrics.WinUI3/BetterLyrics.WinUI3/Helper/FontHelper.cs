using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        private static readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public static string[] SystemFontFamilies => CanvasTextFormat.GetSystemFontFamilies();

        public static string GetUserPreferredFontFamily() => SystemFontFamilies.ElementAtOrDefault(_settingsService.AppSettings.StandardLyricsStyleSettings.SelectedFontFamilyIndex) ?? "Segoe UI";
    }
}
