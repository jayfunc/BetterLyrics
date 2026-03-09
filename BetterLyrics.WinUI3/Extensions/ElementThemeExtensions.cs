using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ElementThemeExtensions
    {
        extension(ElementTheme elementTheme)
        {
            public TitleBarTheme ToTitleBarTheme() => elementTheme switch
            {
                ElementTheme.Light => TitleBarTheme.Light,
                ElementTheme.Dark => TitleBarTheme.Dark,
                _ => TitleBarTheme.UseDefaultAppMode
            };
        }
    }
}
