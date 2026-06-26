using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings
{
    public partial class AlbumArtAreaStyleSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int CoverImageShadowAmount { get; set; } = 12;

        public AlbumArtAreaStyleSettings() { }

        public object Clone()
        {
            return new AlbumArtAreaStyleSettings
            {
                CoverImageRadius = this.CoverImageRadius,
                CoverImageShadowAmount = this.CoverImageShadowAmount,
            };
        }
    }
}
