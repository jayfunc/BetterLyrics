using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Models.Lyrics;

public struct LyricsLayoutMetrics
{
    public float MainLyricsSize;
    public float TranslationSize;
    public float TransliterationSize;

    public float SongTitleSize;
    public float ArtistNameSize;
    public float AlbumNameSize;

    public AppThickness AlbumArtPadding;
}