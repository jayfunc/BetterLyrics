using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class MusicGallerySettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial PlaybackOrder PlaybackOrder { get; set; } = PlaybackOrder.RepeatAll;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> PlayQueuePaths { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PlayQueueIndex { get; set; } = -1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoOpen { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoPlay { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowStatus LyricsWindowStatus { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ExitOnWindowClosed { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool StopOnWindowClosed { get; set; } = false;

        public MusicGallerySettings() { }
    }
}
