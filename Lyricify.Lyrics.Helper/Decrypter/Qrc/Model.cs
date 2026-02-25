namespace Lyricify.Lyrics.Decrypter.Qrc
{
    public class QqLyricsResponse
    {
        public string? Lyrics { get; set; }

        public string? Trans { get; set; }
    }

    public class SongResponse
    {
        public long Code { get; set; }

        public Song[]? Data { get; set; }

        public class Song
        {
            public string? Id { get; set; }
        }
    }
}
