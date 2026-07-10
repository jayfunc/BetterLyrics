using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Data;

namespace BetterLyrics.Avalonia.Controls;

public partial class SemanticZoom : UserControl
{
    public static readonly StyledProperty<Control?> ZoomedInViewProperty =
        AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedInView));

    public static readonly StyledProperty<Control?> ZoomedOutViewProperty =
        AvaloniaProperty.Register<SemanticZoom, Control?>(nameof(ZoomedOutView));

    public static readonly StyledProperty<bool> IsZoomedInViewActiveProperty =
        AvaloniaProperty.Register<SemanticZoom, bool>(nameof(IsZoomedInViewActive), true, defaultBindingMode: BindingMode.TwoWay);

    public SemanticZoom()
    {
        InitializeComponent();
    }

    public Control? ZoomedInView
    {
        get => GetValue(ZoomedInViewProperty);
        set => SetValue(ZoomedInViewProperty, value);
    }

    public Control? ZoomedOutView
    {
        get => GetValue(ZoomedOutViewProperty);
        set => SetValue(ZoomedOutViewProperty, value);
    }

    public bool IsZoomedInViewActive
    {
        get => GetValue(IsZoomedInViewActiveProperty);
        set => SetValue(IsZoomedInViewActiveProperty, value);
    }

    public void ToggleView()
    {
        IsZoomedInViewActive = !IsZoomedInViewActive;
    }
}
