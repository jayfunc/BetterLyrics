using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AlbumArtAreaEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool SongInfoAutoScroll { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ImageSwitchType ImageSwitchType { get; set; } = ImageSwitchType.Slide;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool FadeOut { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Direction FadeOutDirection { get; set; } = Direction.Right;

        public object Clone()
        {
            return new AlbumArtAreaEffectSettings()
            {
                SongInfoAutoScroll = this.SongInfoAutoScroll,
                ImageSwitchType = this.ImageSwitchType,
                FadeOut = this.FadeOut,
                FadeOutDirection = this.FadeOutDirection
            };
        }
    }
}
