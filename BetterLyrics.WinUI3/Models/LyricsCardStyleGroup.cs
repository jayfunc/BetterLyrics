using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsCardStyleGroup : ObservableCollection<LyricsCardStyleItem>
    {
        public string GroupTitle { get; set; }

        public LyricsCardStyleGroup(string title, IEnumerable<LyricsCardStyleItem> items) : base(items)
        {
            GroupTitle = title;
        }
    }
}
