namespace BetterLyrics.Core.Extensions;

public static class EnumExtensions
{
    extension<T>(T value) where T : struct, Enum
    {
        public T GetNext()
        {
            var values = Enum.GetValues<T>();
            var currentIndex = Array.IndexOf(values, value);
            var nextIndex = (currentIndex + 1) % values.Length;
            return values[nextIndex];
        }
    }
}