using System;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class EnumExtensions
    {
        extension<T>(T value) where T : struct, Enum
        {
            public T GetNext()
            {
                T[] values = Enum.GetValues<T>();
                int currentIndex = Array.IndexOf(values, value);
                int nextIndex = (currentIndex + 1) % values.Length;
                return values[nextIndex];
            }
        }
    }
}
