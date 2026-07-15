using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models;

public partial class ScatteredAlbumModel : ObservableObject
{
    [ObservableProperty] public partial AlbumModel Album { get; set; }
    [ObservableProperty] public partial double X { get; set; }
    [ObservableProperty] public partial double Y { get; set; }
    [ObservableProperty] public partial double Rotation { get; set; }
    [ObservableProperty] public partial int ZIndex { get; set; }

    public ScatteredAlbumModel(AlbumModel album)
    {
        Album = album;
    }
}
