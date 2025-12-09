using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using NTextCat.Commons;
using System;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsSearchResult : ObservableObject, ICloneable
    {
        public LyricsSearchProvider Provider { get; set; }
        [ObservableProperty] public partial TranslationSearchProvider? TranslationProvider { get; set; }
        [ObservableProperty] public partial TransliterationSearchProvider? TransliterationProvider { get; set; }

        public string? Raw { get; set; }

        /// <summary>
        /// 翻译也可能位于 <see cref="Raw"/>
        /// </summary>
        public string? Translation { get; set; }

        /// <summary>
        /// 音译也可能位于 <see cref="Raw"/>
        /// </summary>
        public string? Transliteration { get; set; }

        public string? Title { get; set; }
        public string[]? Artists { get; set; }
        public string? Album { get; set; }
        public double? Duration { get; set; }
        [ObservableProperty] public partial int MatchPercentage { get; set; } = -1;
        [ObservableProperty] public partial string Reference { get; set; } = "about:blank";

        public string? SelfPath { get; set; }

        [JsonIgnore] public bool IsFound => !string.IsNullOrEmpty(Raw);

        [JsonIgnore] public LyricsSearchProvider? ProviderIfFound => IsFound ? Provider : null;

        [JsonIgnore] public string? DisplayArtists => Artists?.Join("; ");

        public object Clone()
        {
            return new LyricsSearchResult()
            {
                Album = this.Album,
                Duration = this.Duration,
                Raw = this.Raw,
                Translation = this.Translation,
                Title = this.Title,
                Artists = this.Artists,
                MatchPercentage = this.MatchPercentage,
                Provider = this.Provider,
                Reference = this.Reference
            };
        }

        public void CopyFromSongInfo(SongInfo songInfo)
        {
            Title = songInfo.Title;
            Artists = songInfo.Artists;
            Album = songInfo.Album;
        }
    }
}
