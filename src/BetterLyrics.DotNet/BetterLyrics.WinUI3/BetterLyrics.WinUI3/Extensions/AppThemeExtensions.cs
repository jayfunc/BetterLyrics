using BetterLyrics.Core.Enums;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class AppThemeExtensions
{
    extension(AppTheme appTheme)
    {
        public TitleBarTheme ToTitleBarTheme()
        {
            return appTheme switch
            {
                AppTheme.Light => TitleBarTheme.Light,
                AppTheme.Dark => TitleBarTheme.Dark,
                _ => TitleBarTheme.UseDefaultAppMode
            };
        }

        public ElementTheme ToElementTheme()
        {
            return appTheme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }
}