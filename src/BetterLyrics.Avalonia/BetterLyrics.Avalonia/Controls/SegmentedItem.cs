using Avalonia;
using Avalonia.Controls;

namespace BetterLyrics.Avalonia.Controls;

public class SegmentedItem : ListBoxItem
{
    protected override System.Type StyleKeyOverride => typeof(SegmentedItem);

    public static readonly StyledProperty<object> IconProperty =
        AvaloniaProperty.Register<SegmentedItem, object>(nameof(Icon));

    public object Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}