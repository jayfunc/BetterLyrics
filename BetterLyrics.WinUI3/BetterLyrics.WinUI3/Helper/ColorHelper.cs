namespace BetterLyrics.WinUI3.Helper
{
    public class ColorHelper
    {
        public static Windows.UI.Color LerpColor(Windows.UI.Color a, Windows.UI.Color b, double t)
        {
            byte A = (byte)(a.A + (b.A - a.A) * t);
            byte R = (byte)(a.R + (b.R - a.R) * t);
            byte G = (byte)(a.G + (b.G - a.G) * t);
            byte B = (byte)(a.B + (b.B - a.B) * t);
            return Windows.UI.Color.FromArgb(A, R, G, B);
        }
    }
}
