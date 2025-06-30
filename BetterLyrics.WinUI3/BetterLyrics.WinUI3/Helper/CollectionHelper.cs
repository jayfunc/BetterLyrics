// 2025/6/23 by Zhe Fang

using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Helper
{
    public static class CollectionHelper
    {
        public static T? SafeGet<T>(this IList<T> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count) return default;
            return list[index];
        }
    }
}
