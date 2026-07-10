using global::Avalonia;
using global::Avalonia.Controls;

namespace BetterLyrics.Avalonia.Controls;

public partial class ContributorCard : UserControl
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<ContributorCard, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<ContributorCard, string>(nameof(Description), string.Empty);

    public static readonly StyledProperty<string> AvatarSourceProperty =
        AvaloniaProperty.Register<ContributorCard, string>(nameof(AvatarSource), string.Empty);

    public static readonly StyledProperty<string> BadgesProperty =
        AvaloniaProperty.Register<ContributorCard, string>(nameof(Badges), string.Empty);

    public ContributorCard()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string AvatarSource
    {
        get => GetValue(AvatarSourceProperty);
        set => SetValue(AvatarSourceProperty, value);
    }

    public string Badges
    {
        get => GetValue(BadgesProperty);
        set => SetValue(BadgesProperty, value);
    }
}