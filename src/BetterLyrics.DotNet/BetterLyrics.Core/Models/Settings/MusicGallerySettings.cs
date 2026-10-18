using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.MediaLibrary;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class MusicGallerySettings : ObservableObject
{
    [ObservableProperty]
    public partial PlaybackOrder PlaybackOrder { get; set; } = PlaybackOrder.RepeatAll;

    [ObservableProperty]
    public partial TrackInfoDisplayTarget TrackInfoDisplayTarget { get; set; } = TrackInfoDisplayTarget.PlayingItem;

    [Obsolete][ObservableProperty] public partial ObservableCollection<string> PlayQueuePaths { get; set; } = [];
    [Obsolete][ObservableProperty] public partial int PlayQueueIndex { get; set; } = -1;
    [Obsolete][ObservableProperty] public partial TimeSpan PlaybackPosition { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial bool AutoOpen { get; set; } = false;

    [ObservableProperty]
    public partial bool AutoPlay { get; set; } = false;

    [ObservableProperty]
    public partial int Volume { get; set; } = 100;

    [ObservableProperty]
    public partial LyricsWindowStatus LyricsWindowStatus { get; set; } = new(LyricsWindowMode.Standard);

    [ObservableProperty]
    public partial bool ExitOnWindowClosed { get; set; } = false;

    [ObservableProperty]
    public partial bool StopOnWindowClosed { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowAlbumColumn { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowGenreColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowYearColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowTrackNumberColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowBitrateColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowSampleRateColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowFormatColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowFileSizeColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDateCreatedColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDateModifiedColumn { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDurationColumn { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowFolderColumn { get; set; } = true;

    partial void OnPlaybackOrderChanged(PlaybackOrder value) =>
        WeakReferenceMessenger.Default.Send(new PlaybackOrderChangedMessage(value));
}