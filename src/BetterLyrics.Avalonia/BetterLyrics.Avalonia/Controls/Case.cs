using Avalonia;
using Avalonia.Metadata;

namespace BetterLyrics.Avalonia.Controls;

public class Case : AvaloniaObject
{
    // 用于匹配的值
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<Case, object?>(nameof(Value));

    // 是否为默认项（当所有 Case 都不匹配时显示）
    public static readonly StyledProperty<bool> IsDefaultProperty =
        AvaloniaProperty.Register<Case, bool>(nameof(IsDefault));

    // Case 内部包裹的内容
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<Case, object?>(nameof(Content));

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsDefault
    {
        get => GetValue(IsDefaultProperty);
        set => SetValue(IsDefaultProperty, value);
    }

    [Content] // 标记为 Content，这样在 XAML 里就可以直接往 Case 里塞控件了
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
}