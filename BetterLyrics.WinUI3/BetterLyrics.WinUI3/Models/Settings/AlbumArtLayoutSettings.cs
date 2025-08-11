using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtLayoutSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType SongInfoAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size

        public AlbumArtLayoutSettings() { }
    }
}
