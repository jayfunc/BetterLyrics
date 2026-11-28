using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.LyricsWindow;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class AlbumArtAreaEffectSettings : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial bool SongInfoAutoScroll { get; set; } = false;

    [ObservableProperty]
    public partial ImageSwitchType ImageSwitchType { get; set; } = ImageSwitchType.Slide;

    [ObservableProperty]
    public partial bool FadeOut { get; set; } = false;

    [ObservableProperty]
    public partial float FadeOutStartPointX { get; set; } = 0;

    [ObservableProperty]
    public partial float FadeOutStartPointY { get; set; } = 0;

    [ObservableProperty]
    public partial float FadeOutEndPointX { get; set; } = 1;

    [ObservableProperty]
    public partial float FadeOutEndPointY { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsParallaxEnabled { get; set; } = false;

    partial void OnSongInfoAutoScrollChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new AlbumArtSongInfoAutoScrollChangedMessage(this, value));

    public object Clone()
    {
        return new AlbumArtAreaEffectSettings
        {
            SongInfoAutoScroll = SongInfoAutoScroll,
            ImageSwitchType = ImageSwitchType,
            FadeOut = FadeOut,
            FadeOutStartPointX = FadeOutStartPointX,
            FadeOutStartPointY = FadeOutStartPointY,
            FadeOutEndPointX = FadeOutEndPointX,
            FadeOutEndPointY = FadeOutEndPointY,
            IsParallaxEnabled = IsParallaxEnabled
        };
    }
}