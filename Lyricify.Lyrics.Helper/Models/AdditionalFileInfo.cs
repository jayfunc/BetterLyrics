using Lyricify.Lyrics.Parsers.Models.Spotify;

namespace Lyricify.Lyrics.Models
{
    public interface IAdditionalFileInfo { }

    /// <summary>
    /// 通用附加信息 (适用于 LRC、KRC、QRC、Lyricify Syllable、Lyricify Lines 等多种歌词)
    /// </summary>
    public class GeneralAdditionalInfo : IAdditionalFileInfo
    {
        public List<KeyValuePair<string, string>>? Attributes { get; set; }
    }

    /// <summary>
    /// KRC 附加信息
    /// </summary>
    public class KrcAdditionalInfo : GeneralAdditionalInfo
    {
        public string? Hash { get; set; }
    }

    /// <summary>
    /// 适用于 Spotify 歌词的附加信息
    /// </summary>
    public class SpotifyAdditionalInfo : IAdditionalFileInfo
    {
        public SpotifyAdditionalInfo() { }

        public SpotifyAdditionalInfo(SpotifyLyrics lyrics)
        {
            Provider = lyrics.Provider;
            ProviderLyricsId = lyrics.ProviderLyricsId;
            ProviderDisplayName = lyrics.ProviderDisplayName;
            if (!string.IsNullOrEmpty(lyrics.Language))
                LyricsLanguage = lyrics.Language;
        }

        public SpotifyAdditionalInfo(string provider, string providerLyricsId, string providerDisplayName)
        {
            Provider = provider;
            ProviderLyricsId = providerLyricsId;
            ProviderDisplayName = providerDisplayName;
        }

        public SpotifyAdditionalInfo(string provider, string providerLyricsId, string providerDisplayName, string? language) : this(provider, providerLyricsId, providerDisplayName)
        {
            LyricsLanguage = language;
        }

        public string? Provider { get; }

        public string? ProviderLyricsId { get; }

        public string? ProviderDisplayName { get; }

        public string? LyricsLanguage { get; }
    }
}
