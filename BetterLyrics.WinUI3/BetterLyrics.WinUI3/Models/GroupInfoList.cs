using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class GroupInfoList(IEnumerable<object> items) : List<object>(items)
    {
        public required object Key { get; set; }

        public override string ToString()
        {
            return "Group " + Key.ToString();
        }
    }
}
