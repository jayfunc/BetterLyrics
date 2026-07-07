using Avalonia;
using Avalonia.Controls;

namespace BetterLyrics.Avalonia.Controls;

public partial class PatronControl : UserControl
{
    public static readonly StyledProperty<string> PatronNameProperty =
        AvaloniaProperty.Register<PatronControl, string>(nameof(PatronName), string.Empty);

    public static readonly StyledProperty<string> DateProperty =
        AvaloniaProperty.Register<PatronControl, string>(nameof(Date), string.Empty);

    public PatronControl()
    {
        InitializeComponent();
    }

    public string PatronName
    {
        get => GetValue(PatronNameProperty);
        set => SetValue(PatronNameProperty, value);
    }

    public string Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }
}