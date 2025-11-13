using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public static class CollectionHelper
    {
        public static ObservableCollection<GroupInfoList> GetGroupedBy<T>(
            this IEnumerable<T> items,
            Func<T, object> groupKeySelector,
            Func<object, object>? orderSelector = null)
        {
            var query = from item in items
                        group item by groupKeySelector(item) into g
                        orderby g.Key
                        select new GroupInfoList(g.Cast<object>(), orderSelector) { Key = g.Key };

            return new ObservableCollection<GroupInfoList>(query);
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null) return;
            if (items == null) return;
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public static void InsertRange<T>(this IList<T> list, int index, IEnumerable<T> items)
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
