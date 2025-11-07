using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class MusicGallerySettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial PlaybackOrder PlaybackOrder { get; set; } = PlaybackOrder.RepeatAll;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ObservableCollection<string> PlayQueuePaths { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PlayQueueIndex { get; set; } = -1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoOpen { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoPlay { get; set; } = false;

        public MusicGallerySettings() { }
    }
}
