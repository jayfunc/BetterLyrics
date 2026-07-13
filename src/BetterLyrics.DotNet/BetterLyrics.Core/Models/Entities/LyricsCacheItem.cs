using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.Entities;

[Table("LyricsCache")]
// 建立联合索引，确保同一个 Provider 下，同一个 Hash 只有一条记录
[Index(nameof(CacheKey), nameof(Provider), IsUnique = false)]
public partial class LyricsCacheItem : ObservableObject, ICloneable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(64)] [Required] public string CacheKey { get; set; }

    public LyricsSearchProvider Provider { get; set; }
    [ObservableProperty] public partial TranslationSearchProvider? TranslationProvider { get; set; }
    [ObservableProperty] public partial TransliterationSearchProvider? TransliterationProvider { get; set; }

    public string? Raw { get; set; }

    /// <summary>
    ///     翻译也可能位于 <see cref="Raw" />
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    ///     音译也可能位于 <see cref="Raw" />
    /// </summary>
    public string? Transliteration { get; set; }

    [MaxLength(255)] public string? Title { get; set; }

    [MaxLength(255)] public string? Artist { get; set; }

    [MaxLength(255)] public string? Album { get; set; }

    public double? Duration { get; set; }
    [ObservableProperty] public partial int MatchPercentage { get; set; } = -1;
    [ObservableProperty] public partial string Reference { get; set; } = "about:blank";

    [NotMapped] [JsonIgnore] public bool IsFound => !string.IsNullOrEmpty(Raw);
    [NotMapped] [JsonIgnore] public bool IsPlugin => Provider.IsPlugin();
    [NotMapped] [JsonIgnore] public LyricsSearchProvider? ProviderIfFound => IsFound ? Provider : null;
    [NotMapped] [JsonIgnore] public bool IsSearching { get; set; } = false;

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