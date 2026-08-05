using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteDB;

namespace BetterLyrics.Core.Models.Entities;

public partial class LyricsCacheItem : ObservableObject, ICloneable
{
    public int Id { get; set; }

    public string CacheKey { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTranslationIntrinsic))]
    [NotifyPropertyChangedFor(nameof(IsTranslationGenerated))]
    [NotifyPropertyChangedFor(nameof(IsTransliterationIntrinsic))]
    [NotifyPropertyChangedFor(nameof(IsTransliterationGenerated))]
    public partial LyricsProvider Provider { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTranslationIntrinsic))]
    [NotifyPropertyChangedFor(nameof(IsTranslationGenerated))]
    [NotifyPropertyChangedFor(nameof(IsTranslationNotAvailable))]
    public partial LyricsProvider? TranslationProvider { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTransliterationIntrinsic))]
    [NotifyPropertyChangedFor(nameof(IsTransliterationGenerated))]
    [NotifyPropertyChangedFor(nameof(IsTransliterationNotAvailable))]
    public partial LyricsProvider? TransliterationProvider { get; set; }

    public string? Raw { get; set; }

    /// <summary>
    ///     翻译也可能位于 <see cref="Raw" />
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    ///     音译也可能位于 <see cref="Raw" />
    /// </summary>
    public string? Transliteration { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Album { get; set; }

    public double? Duration { get; set; }
    [ObservableProperty] public partial int MatchPercentage { get; set; } = -1;
    [ObservableProperty] public partial string Reference { get; set; } = "about:blank";

    [JsonIgnore][BsonIgnore] public bool IsFound => !string.IsNullOrEmpty(Raw);
    [JsonIgnore][BsonIgnore] public bool IsPlugin => Provider.IsPlugin();
    [JsonIgnore][BsonIgnore] public LyricsProvider? ProviderIfFound => IsFound ? Provider : null;
    [JsonIgnore][BsonIgnore] public bool IsSearching { get; set; } = false;

    [JsonIgnore]
    [BsonIgnore]
    [ObservableProperty]

    [NotifyPropertyChangedFor(nameof(IsWordByWord))]
    public partial List<LyricsData>? LyricsDataArr { get; set; }

    [JsonIgnore][BsonIgnore] public bool IsTranslationIntrinsic => TranslationProvider != null && TranslationProvider == Provider;
    [JsonIgnore][BsonIgnore] public bool IsTranslationGenerated => TranslationProvider != null && TranslationProvider != Provider;
    [JsonIgnore][BsonIgnore] public bool IsTranslationNotAvailable => TranslationProvider == null;

    [JsonIgnore][BsonIgnore] public bool IsTransliterationIntrinsic => TransliterationProvider != null && TransliterationProvider == Provider;
    [JsonIgnore][BsonIgnore] public bool IsTransliterationGenerated => TransliterationProvider != null && TransliterationProvider != Provider;
    [JsonIgnore][BsonIgnore] public bool IsTransliterationNotAvailable => TransliterationProvider == null;

    [JsonIgnore][BsonIgnore] public bool IsWordByWord => LyricsDataArr?.FirstOrDefault()?.IsWordByWord ?? false;

    public object Clone()
    {
        return new LyricsCacheItem
        {
            Provider = Provider,
            TranslationProvider = TranslationProvider,
            TransliterationProvider = TransliterationProvider,

            Raw = Raw,
            Translation = Translation,
            Transliteration = Transliteration,

            Title = Title,
            Artist = Artist,
            Album = Album,
            Duration = Duration,

            MatchPercentage = MatchPercentage,
            Reference = Reference
        };
    }

    public void CopyFromSongInfo(SongInfo songInfo)
    {
        Title = songInfo.Title;
        Artist = songInfo.Artist;
        Album = songInfo.Album;
    }
}