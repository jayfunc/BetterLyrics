using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MappedSongSearchQuery : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalTitle { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalArtist { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalAlbum { get; set; } = string.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedTitle { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedArtist { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedAlbum { get; set; } = string.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsMarkedAsPureMusic { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider? LyricsSearchProvider { get; set; }

        public MappedSongSearchQuery Clone()
        {
            return new MappedSongSearchQuery
            {
                OriginalTitle = this.OriginalTitle,
                OriginalArtist = this.OriginalArtist,
                OriginalAlbum = this.OriginalAlbum,
                MappedTitle = this.MappedTitle,
                MappedArtist = this.MappedArtist,
                MappedAlbum = this.MappedAlbum,
                IsMarkedAsPureMusic = this.IsMarkedAsPureMusic,
                LyricsSearchProvider = this.LyricsSearchProvider
            };
        }
    }
}
