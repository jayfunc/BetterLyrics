using System;
using global::Avalonia;
using global::Avalonia.Collections;
using global::Avalonia.Controls;
using global::Avalonia.Metadata;

namespace BetterLyrics.Avalonia.Controls;

public class SwitchPresenter : TransitioningContentControl
{
    // 绑定的当前状态值
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<SwitchPresenter, object?>(nameof(Value));

    // Case 集合
    public static readonly DirectProperty<SwitchPresenter, AvaloniaList<Case>> CasesProperty =
        AvaloniaProperty.RegisterDirect<SwitchPresenter, AvaloniaList<Case>>(
            nameof(Cases),
            o => o.Cases);

    public SwitchPresenter()
    {
        // 当集合变化时，重新评估应该显示哪个 Case
        Cases.CollectionChanged += (s, e) => EvaluateCases();
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // 重写了 XAML 的默认内容接收器，把子元素全部装进 Cases 列表，而不是直接渲染
    [Content] public AvaloniaList<Case> Cases { get; } = new();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 当绑定的 Value 发生变化时，触发状态切换
        if (change.Property == ValueProperty) EvaluateCases();
    }

    private void EvaluateCases()
    {
        if (Cases == null || Cases.Count == 0) return;

        Case? matchedCase = null;
        Case? defaultCase = null;

        foreach (var c in Cases)
        {
            if (c.IsDefault) defaultCase = c;

            // 比较绑定的 Value 和 Case 的 Value
            if (CompareValues(Value, c.Value))
            {
                matchedCase = c;
                break;
            }
        }

        // 最终决定显示的内容，赋值给父类 TransitioningContentControl 的 Content，自动触发动画
        Content = (matchedCase ?? defaultCase)?.Content;
    }

    // 类型比对兼容器（解决 XAML 中填写的 Value 是字符串，而后台是枚举/整数的冲突）
    private bool CompareValues(object? val1, object? val2)
    {
        if (Equals(val1, val2)) return true;
        if (val1 == null || val2 == null) return false;

        var type1 = val1.GetType();

        // 兼容枚举处理
        if (type1.IsEnum && val2 is string strVal)
            try
            {
                return Equals(val1, Enum.Parse(type1, strVal, true));
            }
            catch
            {
                return false;
            }

        // 兼容字符串与数值的转换处理
        try
        {
            var converted = Convert.ChangeType(val2, type1);
            return Equals(val1, converted);
        }
        catch
        {
            // 纯字符串比较
            return val1.ToString() == val2.ToString();
        }
    }
}