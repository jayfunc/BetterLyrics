using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ContributorCard : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(ContributorCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ContributorCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AvatarSourceProperty =
        DependencyProperty.Register(nameof(AvatarSource), typeof(string), typeof(ContributorCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BadgesProperty =
        DependencyProperty.Register(nameof(Badges), typeof(string), typeof(ContributorCard),
            new PropertyMetadata(string.Empty));

    public ContributorCard()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string AvatarSource
    {
        get => (string)GetValue(AvatarSourceProperty);
        set => SetValue(AvatarSourceProperty, value);
    }

    public string Badges
    {
        get => (string)GetValue(BadgesProperty);
        set => SetValue(BadgesProperty, value);
    }
}