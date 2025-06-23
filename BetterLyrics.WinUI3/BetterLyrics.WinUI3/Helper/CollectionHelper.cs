// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="CollectionHelper" />
    /// </summary>
    public static class CollectionHelper
    {
        #region Methods

        /// <summary>
        /// The SafeGet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list<see cref="IList{T}"/></param>
        /// <param name="index">The index<see cref="int"/></param>
        /// <returns>The <see cref="T?"/></returns>
        public static T? SafeGet<T>(this IList<T> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return default;
            return list[index];
        }

        #endregion
    }
}
