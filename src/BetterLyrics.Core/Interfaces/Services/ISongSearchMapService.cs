using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ISongSearchMapService
{
    Task SaveMappingAsync(MappedSongSearchQuery mapping);
    Task<MappedSongSearchQuery?> TryGetMappingAsync(SongInfo songInfo, CancellationToken token = default);

    Task<(string Title, string Artist, string Album)> GetMappingAsync(SongInfo songInfo,
        CancellationToken token = default);

    Task DeleteMappingAsync(MappedSongSearchQuery mapping);
}