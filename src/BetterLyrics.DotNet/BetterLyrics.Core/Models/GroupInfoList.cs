namespace BetterLyrics.Core.Models;

public class GroupInfoList : List<object>
{
    public GroupInfoList(IEnumerable<object> items, Func<object, object>? orderSelector = null, bool isDescending = false)
        : base(orderSelector != null
            ? (isDescending ? items.OrderByDescending(orderSelector) : items.OrderBy(orderSelector))
            : items)
    {
    }

    public required object Key { get; set; }

    public override string ToString()
    {
        return $"{Key}";
    }
}