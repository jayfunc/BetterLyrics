using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class ElementThemeExtensions
{
    extension(ElementTheme elementTheme)
    {
        public TitleBarTheme ToTitleBarTheme()
        {
            return elementTheme switch
            {
                ElementTheme.Light => TitleBarTheme.Light,
                ElementTheme.Dark => TitleBarTheme.Dark,
                _ => TitleBarTheme.UseDefaultAppMode
            };
        }
    }
}