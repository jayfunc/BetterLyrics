using ATL;
using BetterLyrics.WinUI3.Models;
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
        public static ObservableCollection<GroupInfoList> GetGroupedByTitleAsync(this ICollection<Track> tracks)
        {
            // Grab Contact objects from pre-existing list (list is returned from function GetContactsAsync())
            var query = from item in tracks

                            // Group the items returned from the query, sort and select the ones you want to keep
                        group item by item.Title.Substring(0, 1).ToUpper() into g
                        orderby g.Key

                        // GroupInfoList is a simple custom class that has an IEnumerable type attribute, and
                        // a key attribute. The IGrouping-typed variable g now holds the Contact objects,
                        // and these objects will be used to create a new GroupInfoList object.
                        select new GroupInfoList(g) { Key = g.Key };

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
