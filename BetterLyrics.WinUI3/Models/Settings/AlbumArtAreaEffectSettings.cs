using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtAreaEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool SongInfoAutoScroll { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ImageSwitchType ImageSwitchType { get; set; } = ImageSwitchType.Slide;

        public object Clone()
        {
            return new AlbumArtAreaEffectSettings()
            {
                SongInfoAutoScroll = this.SongInfoAutoScroll,
                ImageSwitchType = this.ImageSwitchType,
            };
        }
    }
}
