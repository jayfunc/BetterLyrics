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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial float FadeOutStartPointX { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial float FadeOutStartPointY { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial float FadeOutEndPointX { get; set; } = 1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial float FadeOutEndPointY { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsParallaxEnabled { get; set; } = false;

        public object Clone()
        {
            return new AlbumArtAreaEffectSettings()
            {
                SongInfoAutoScroll = this.SongInfoAutoScroll,
                ImageSwitchType = this.ImageSwitchType,
                FadeOut = this.FadeOut,
                FadeOutStartPointX = this.FadeOutStartPointX,
                FadeOutStartPointY = this.FadeOutStartPointY,
                FadeOutEndPointX = this.FadeOutEndPointX,
                FadeOutEndPointY = this.FadeOutEndPointY,
                IsParallaxEnabled = this.IsParallaxEnabled,
            };
        }
    }
}
