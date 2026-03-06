using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BetterLyrics.WinUI3.Models
{
    [Table("SongSearchMap")]
    [Index(nameof(OriginalTitle), nameof(OriginalArtist), nameof(OriginalAlbum))]
    public partial class MappedSongSearchQuery : ObservableRecipient, ICloneable
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)] public string Id { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalTitle { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalArtist { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string OriginalAlbum { get; set; } = string.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedTitle { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedArtist { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MappedAlbum { get; set; } = string.Empty;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsMarkedAsPureMusic { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSearchProvider? LyricsSearchProvider { get; set; }

        public object Clone()
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

        public override string ToString()
        {
            return
                $"Title: {OriginalTitle} -> {MappedTitle} " +
                $"Artist: {OriginalArtist} -> {MappedArtist} " +
                $"Album: {OriginalAlbum} -> {MappedAlbum}";
        }
    }
}
