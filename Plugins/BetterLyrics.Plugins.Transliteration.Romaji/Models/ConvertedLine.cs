using System.Collections.ObjectModel;

namespace BetterLyrics.Plugins.Transliteration.Romaji.Models
{
    public class ConvertedLine
    {
        public ushort Index { get; set; } = 0;

        public TimeSpan? Time { get; set; }

        public string Chinese { get; set; } = string.Empty;

        public string Japanese { get; set; } = string.Empty;

        public ObservableCollection<ConvertedUnit> Units { get; set; } = new ObservableCollection<ConvertedUnit>();
    }
}