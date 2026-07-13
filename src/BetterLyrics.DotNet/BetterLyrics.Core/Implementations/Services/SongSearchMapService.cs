using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Implementations.Services;

public class SongSearchMapService : ISongSearchMapService
{
    private readonly IDbContextFactory<SongSearchMapDbContext> _contextFactory;

    public SongSearchMapService(IDbContextFactory<SongSearchMapDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SaveMappingAsync(MappedSongSearchQuery mapping)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.SongSearchMap
            .FirstOrDefaultAsync(x =>
                x.OriginalTitle == mapping.OriginalTitle &&
                x.OriginalArtist == mapping.OriginalArtist &&
                x.OriginalAlbum == mapping.OriginalAlbum);

        if (existing != null)
        {
            existing.MappedTitle = mapping.MappedTitle;
            existing.MappedArtist = mapping.MappedArtist;
            existing.MappedAlbum = mapping.MappedAlbum;

            existing.IsMarkedAsPureMusic = mapping.IsMarkedAsPureMusic;
            existing.LyricsSearchProvider = mapping.LyricsSearchProvider;

            context.SongSearchMap.Update(existing);
        }
        else
        {
            var newItem = (MappedSongSearchQuery)mapping.Clone();
            await context.SongSearchMap.AddAsync(newItem);
        }

        await context.SaveChangesAsync();
    }

    public async Task<MappedSongSearchQuery?> TryGetMappingAsync(SongInfo songInfo, CancellationToken token = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync(token);

        return await context.SongSearchMap
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.OriginalTitle == songInfo.Title &&
                x.OriginalArtist == songInfo.Artist &&
                x.OriginalAlbum == songInfo.Album, token);
    }

    public async Task<(string Title, string Artist, string Album)> GetMappingAsync(SongInfo songInfo,
        CancellationToken token = default)
    {
        var mappedTitle = songInfo.Title;
        var mappedArtist = songInfo.Artist;
        var mappedAlbum = songInfo.Album;

        var mapped = await TryGetMappingAsync(songInfo, token);

        if (mapped != null)
        {
            mappedTitle = mapped.MappedTitle;
            mappedArtist = mapped.MappedArtist;
            mappedAlbum = mapped.MappedAlbum;
        }

        return (mappedTitle, mappedArtist, mappedAlbum);
    }

    public async Task DeleteMappingAsync(MappedSongSearchQuery mapping)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var target = await context.SongSearchMap
            .FirstOrDefaultAsync(x =>
                x.OriginalTitle == mapping.OriginalTitle &&
                x.OriginalArtist == mapping.OriginalArtist &&
                x.OriginalAlbum == mapping.OriginalAlbum);

        if (target != null)
        {
            context.SongSearchMap.Remove(target);
            await context.SaveChangesAsync();
        }
    }
}