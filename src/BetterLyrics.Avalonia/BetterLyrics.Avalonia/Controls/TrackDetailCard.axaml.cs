using global::Avalonia;
using global::Avalonia.Controls;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Avalonia.Controls;

public partial class TrackDetailCard : UserControl
{
    public static readonly StyledProperty<ExtendedTrack> ExtendedTrackProperty =
        AvaloniaProperty.Register<TrackDetailCard, ExtendedTrack>(nameof(ExtendedTrack));

    public TrackDetailCard()
    {
        InitializeComponent();
    }

    public ExtendedTrack ExtendedTrack
    {
        get => GetValue(ExtendedTrackProperty);
        set => SetValue(ExtendedTrackProperty, value);
    }
}