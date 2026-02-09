using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtAreaStyleSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TextAlignmentType SongInfoAlignmentType { get; set; } = TextAlignmentType.Left;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAutoCoverImageHeight { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageHeight { get; set; } = 128;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageShadowAmount { get; set; } = 12;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAutoSongInfoFontSize { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SongInfoFontSize { get; set; } = 18;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowTitle { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowArtists { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowAlbum { get; set; } = false;

        public AlbumArtAreaStyleSettings() { }

        public object Clone()
        {
            return new AlbumArtAreaStyleSettings
            {
                SongInfoAlignmentType = this.SongInfoAlignmentType,

                IsAutoCoverImageHeight = this.IsAutoCoverImageHeight,
                CoverImageHeight = this.CoverImageHeight,
                CoverImageRadius = this.CoverImageRadius,
                CoverImageShadowAmount = this.CoverImageShadowAmount,

                IsAutoSongInfoFontSize = this.IsAutoSongInfoFontSize,
                SongInfoFontSize = this.SongInfoFontSize,

                ShowTitle = this.ShowTitle,
                ShowArtists = this.ShowArtists,
                ShowAlbum = this.ShowAlbum,
            };
        }
    }
}
