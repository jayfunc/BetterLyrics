using BetterLyrics.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class TrackDetailCard : UserControl
{
    public static readonly DependencyProperty ExtendedTrackProperty =
        DependencyProperty.Register(nameof(ExtendedTrack), typeof(ExtendedTrack), typeof(TrackDetailCard),
            new PropertyMetadata(null));

    public TrackDetailCard()
    {
        InitializeComponent();
    }

    public ExtendedTrack ExtendedTrack
    {
        get => (ExtendedTrack)GetValue(ExtendedTrackProperty);
        set => SetValue(ExtendedTrackProperty, value);
    }
}