using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtLayoutSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType SongInfoAlignmentType { get; set; } = TextAlignmentType.Left;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageShadowAmount { get; set; } = 12;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAutoSongInfoFontSize { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SongInfoFontSize { get; set; } = 18;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowTitle { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowArtists { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowAlbum { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int AlbumArtSize { get; set; } = 64;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoAlbumArtSize { get; set; } = true;

        public AlbumArtLayoutSettings() { }

        public object Clone()
        {
            return new AlbumArtLayoutSettings
            {
                SongInfoAlignmentType = this.SongInfoAlignmentType,
                CoverImageRadius = this.CoverImageRadius,
                CoverImageShadowAmount = this.CoverImageShadowAmount,
                IsAutoSongInfoFontSize = this.IsAutoSongInfoFontSize,
                SongInfoFontSize = this.SongInfoFontSize,
                ShowTitle = this.ShowTitle,
                ShowArtists = this.ShowArtists,
                ShowAlbum = this.ShowAlbum,
                AlbumArtSize = this.AlbumArtSize,
                AutoAlbumArtSize = this.AutoAlbumArtSize,
            };
        }
    }
}
