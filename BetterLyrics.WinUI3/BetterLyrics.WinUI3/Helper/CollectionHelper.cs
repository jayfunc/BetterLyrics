using ATL;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

    }
}
