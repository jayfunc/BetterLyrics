using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IAlbumArtSearchService
{
    Task<byte[]?> SearchAsync(SongInfo songInfo, byte[]? bufferFromSMTC, bool ignoreCache, CancellationToken token);
    Task<string?> GetAlbumArtUrlAsync(SongInfo songInfo, DiscordAlbumArtSource source, int size, CancellationToken token);
}