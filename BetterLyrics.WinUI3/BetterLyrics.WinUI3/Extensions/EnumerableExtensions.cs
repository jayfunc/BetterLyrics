using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class EnumerableExtensions
    {
        extension<T>(IEnumerable<T> items)
        {
            public ObservableCollection<GroupInfoList> GetGroupedBy(Func<T, object> groupKeySelector, Func<object, object>? orderSelector = null)
            {
                var query = from item in items
                    group item by groupKeySelector(item) into g
                    orderby g.Key
                    select new GroupInfoList(g.Cast<object>(), orderSelector) { Key = g.Key };

                return new ObservableCollection<GroupInfoList>(query);
            }

        }
    }
}
