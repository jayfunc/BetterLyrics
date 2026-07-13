using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class ThicknessExtensions
{
    extension(Thickness thickness)
    {
        public Thickness WithLeft(double val)
        {
            return new Thickness(val, thickness.Top, thickness.Right, thickness.Bottom);
        }

        public Thickness WithTop(double val)
        {
            return new Thickness(thickness.Left, val, thickness.Right, thickness.Bottom);
        }

        public Thickness WithRight(double val)
        {
            return new Thickness(thickness.Left, thickness.Top, val, thickness.Bottom);
        }

        public Thickness WithBottom(double val)
        {
            return new Thickness(thickness.Left, thickness.Top, thickness.Right, val);
        }
    }
}