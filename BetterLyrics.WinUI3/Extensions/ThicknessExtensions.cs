using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ThicknessExtensions
    {
        extension(Thickness thickness)
        {
            public Thickness WithLeft(int val) => new Thickness(val, thickness.Top, thickness.Right, thickness.Bottom);
            public Thickness WithTop(int val) => new Thickness(thickness.Left, val, thickness.Right, thickness.Bottom);
            public Thickness WithRight(int val) => new Thickness(thickness.Left, thickness.Top, val, thickness.Bottom);
            public Thickness WithBottom(int val) => new Thickness(thickness.Left, thickness.Top, thickness.Right, val);
        }
    }
}
