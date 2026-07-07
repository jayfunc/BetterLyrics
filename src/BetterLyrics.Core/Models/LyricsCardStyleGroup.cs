using System.Collections.ObjectModel;

namespace BetterLyrics.Core.Models;

public class LyricsCardStyleGroup : ObservableCollection<LyricsCardStyleItem>
{
    public LyricsCardStyleGroup(string title, IEnumerable<LyricsCardStyleItem> items) : base(items)
    {
        GroupTitle = title;
    }

    public string GroupTitle { get; set; }
}