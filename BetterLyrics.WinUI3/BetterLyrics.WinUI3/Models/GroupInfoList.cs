using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class GroupInfoList : List<object>
    {
        public required object Key { get; set; }

        public GroupInfoList(IEnumerable<object> items, Func<object, object>? orderSelector = null)
            : base(orderSelector != null
                ? items.OrderBy(orderSelector)
                : items)
        {
        }

        public override string ToString()
        {
            return $"{Key}";
        }
    }
}
