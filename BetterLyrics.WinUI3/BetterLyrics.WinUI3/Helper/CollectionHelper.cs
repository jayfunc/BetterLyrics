using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper {
    public static class CollectionHelper {
        public static T? SafeGet<T>(this IList<T> list, int index) {
            if (list == null || index < 0 || index >= list.Count)
                return default;
            return list[index];
        }
    }
}
