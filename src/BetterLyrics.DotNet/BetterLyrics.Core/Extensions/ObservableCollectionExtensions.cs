using System.Collections.ObjectModel;

namespace BetterLyrics.Core.Extensions;

public static class ObservableCollectionExtensions
{
    public static void InsertRange<T>(this ObservableCollection<T> collection, int index, IEnumerable<T> items)
    {
        if (collection == null) return;
        if (items == null) return;
        if (index < 0 || index > collection.Count) return;

        foreach (var item in items) collection.Insert(index++, item);
    }

    public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
    {
        var sorted = collection.ToList();
        sorted.Sort(comparison);

        for (var i = 0; i < sorted.Count; i++)
        {
            var oldIndex = collection.IndexOf(sorted[i]);
            var newIndex = i;

            if (oldIndex != newIndex) collection.Move(oldIndex, newIndex);
        }
    }

    public static void Sort<T, TKey>(this ObservableCollection<T> collection, Func<T, TKey> keySelector,
        bool descending = false)
    {
        Comparison<T> comparison = (x, y) =>
        {
            var keyX = keySelector(x);
            var keyY = keySelector(y);

            return descending
                ? Comparer<TKey>.Default.Compare(keyY, keyX)
                : Comparer<TKey>.Default.Compare(keyX, keyY);
        };

        collection.Sort(comparison);
    }
}