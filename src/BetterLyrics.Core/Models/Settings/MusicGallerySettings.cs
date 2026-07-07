using System.Collections.ObjectModel;
using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class MusicGallerySettings : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackOrder PlaybackOrder { get; set; } = PlaybackOrder.RepeatAll;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TrackInfoDisplayTarget TrackInfoDisplayTarget { get; set; } = TrackInfoDisplayTarget.PlayingItem;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial ObservableCollection<string> PlayQueuePaths { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PlayQueueIndex { get; set; } = -1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TimeSpan PlaybackPosition { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool AutoOpen { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool AutoPlay { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsWindowStatus LyricsWindowStatus { get; set; } = new(LyricsWindowMode.Standard);

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ExitOnWindowClosed { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool StopOnWindowClosed { get; set; } = false;
}