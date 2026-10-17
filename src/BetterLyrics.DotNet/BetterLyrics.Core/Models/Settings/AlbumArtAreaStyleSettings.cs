using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AlbumArtAreaStyleSettings : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial int CoverImageRadius { get; set; } = 12; // 12 % of the cover image size

    [ObservableProperty]
    public partial int CoverImageShadowAmount { get; set; } = 12;

    public object Clone()
    {
        return new AlbumArtAreaStyleSettings
        {
            CoverImageRadius = CoverImageRadius,
            CoverImageShadowAmount = CoverImageShadowAmount
        };
    }
}