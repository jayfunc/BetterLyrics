using Avalonia.Styling;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Avalonia.Extensions;

public static class AppThemeExtensions
{
    public static ThemeVariant ToThemeVariant(this AppTheme appTheme)
    {
        return appTheme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Default => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }
}
