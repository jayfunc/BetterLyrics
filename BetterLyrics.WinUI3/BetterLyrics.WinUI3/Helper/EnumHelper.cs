using System;

namespace BetterLyrics.WinUI3.Helper
{
    public static class EnumExtensions
    {
        public static T GetNext<T>(this T value) where T : struct, Enum
        {
            T[] values = Enum.GetValues<T>();
            int currentIndex = Array.IndexOf(values, value);
            int nextIndex = (currentIndex + 1) % values.Length;
            return values[nextIndex];
        }
    }
}
