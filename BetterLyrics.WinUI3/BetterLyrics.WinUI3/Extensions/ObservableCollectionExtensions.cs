using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ObservableCollectionExtensions
    {
        extension<T>(ObservableCollection<T> list)
        {
            public void InsertRange(int index, IEnumerable<T> items)
            {
                if (list == null) return;
                if (items == null) return;
                if (index < 0 || index > list.Count) return;
                foreach (var item in items)
                {
                    list.Insert(index++, item);
                }
            }
        }
    }
}
