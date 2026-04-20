using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Helper
{
    public static class BrushHelper
    {
        public static Brush GetThemeBrush(FrameworkElement frameworkElement, string key)
        {
            if (frameworkElement.Resources.TryGetValue(key, out var resource) && resource is Brush brush)
            {
                return brush;
            }
            return new SolidColorBrush(Colors.Gray);
        }
    }
}
