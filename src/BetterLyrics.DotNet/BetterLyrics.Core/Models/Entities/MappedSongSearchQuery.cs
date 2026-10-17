using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB;

namespace BetterLyrics.Core.Models.Entities;

public partial class MappedSongSearchQuery : ObservableObject, ICloneable
{
    public ObjectId Id { get; set; }

    [ObservableProperty]
    public partial string OriginalTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginalArtist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginalAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MappedTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MappedArtist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MappedAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMarkedAsPureMusic { get; set; } = false;

    [ObservableProperty]
    public partial LyricsProvider? LyricsSearchProvider { get; set; }

    public object Clone()
    {
        return new MappedSongSearchQuery
        {
            OriginalTitle = OriginalTitle,
            OriginalArtist = OriginalArtist,
            OriginalAlbum = OriginalAlbum,
            MappedTitle = MappedTitle,
            MappedArtist = MappedArtist,
            MappedAlbum = MappedAlbum,
            IsMarkedAsPureMusic = IsMarkedAsPureMusic,
            LyricsSearchProvider = LyricsSearchProvider
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