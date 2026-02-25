namespace Lyricify.Lyrics.Models
{
    public interface ISyllableInfo
    {
        public string Text { get; }

        public int StartTime { get; }

        public int EndTime { get; }

        public int Duration => EndTime - StartTime;
    }
}
