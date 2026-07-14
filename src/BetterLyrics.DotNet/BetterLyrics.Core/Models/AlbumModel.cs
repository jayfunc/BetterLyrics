namespace BetterLyrics.Core.Models;

public class AlbumModel
{
    public required string Title { get; set; }
    public string? LocalAlbumArtPath { get; set; }
    public int SongCount { get; set; }
}
