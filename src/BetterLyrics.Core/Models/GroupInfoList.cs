namespace BetterLyrics.Core.Models;

public class GroupInfoList : List<object>
{
    public GroupInfoList(IEnumerable<object> items, Func<object, object>? orderSelector = null)
        : base(orderSelector != null
            ? items.OrderBy(orderSelector)
            : items)
    {
    }

    public required object Key { get; set; }

    public override string ToString()
    {
        return $"{Key}";
    }
}