using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PatronControl : UserControl
{
    public static readonly DependencyProperty PatronNameProperty =
        DependencyProperty.Register(nameof(PatronName), typeof(string), typeof(PatronControl),
            new PropertyMetadata(""));

    public static readonly DependencyProperty DateProperty =
        DependencyProperty.Register(nameof(Date), typeof(string), typeof(PatronControl), new PropertyMetadata(""));

    public PatronControl()
    {
        InitializeComponent();
    }

    public string PatronName
    {
        get => (string)GetValue(PatronNameProperty);
        set => SetValue(PatronNameProperty, value);
    }

    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }
}