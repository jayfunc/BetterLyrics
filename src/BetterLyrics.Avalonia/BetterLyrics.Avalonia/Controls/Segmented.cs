using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace BetterLyrics.Avalonia.Controls;

public class Segmented : ListBox
{
    protected override System.Type StyleKeyOverride => typeof(Segmented);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new SegmentedItem();
    }
}