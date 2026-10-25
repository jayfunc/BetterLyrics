using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class SongSearchMapService : ISongSearchMapService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<SongSearchMapService> _logger;

    public SongSearchMapService(IDatabaseService databaseService, ILogger<SongSearchMapService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;

        var col = _databaseService.SongSearchMapDb.GetCollection<MappedSongSearchQuery>("songSearchMap");

        try
        {
            col.DropIndex("OriginalTitle");
            col.DropIndex("OriginalArtist");
            col.DropIndex("OriginalAlbum");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to drop legacy song search map indexes.");
        }
    }

    private ILiteCollection<MappedSongSearchQuery> GetCollection()
    {
        return _databaseService.SongSearchMapDb.GetCollection<MappedSongSearchQuery>("songSearchMap");
    }

    public Task SaveMappingAsync(MappedSongSearchQuery mapping)
    {
        var col = GetCollection();

        var existing = col.FindOne(x =>
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

            col.Update(existing);
            _logger.LogDebug("Updated song search mapping: {OriginalTitle} - {OriginalArtist} -> {MappedTitle} - {MappedArtist}.",
                existing.OriginalTitle, existing.OriginalArtist, existing.MappedTitle, existing.MappedArtist);
        }
        else
        {
            var newItem = (MappedSongSearchQuery)mapping.Clone();
            col.Insert(newItem);
            _logger.LogDebug("Created song search mapping: {OriginalTitle} - {OriginalArtist} -> {MappedTitle} - {MappedArtist}.",
                newItem.OriginalTitle, newItem.OriginalArtist, newItem.MappedTitle, newItem.MappedArtist);
        }

        return Task.CompletedTask;
    }

    public async Task<MappedSongSearchQuery?> TryGetMappingAsync(SongInfo songInfo, CancellationToken token = default)
    {
        var col = GetCollection();

        var mapped = col.FindOne(x =>
                x.OriginalTitle == songInfo.Title &&
                x.OriginalArtist == songInfo.Artist &&
                x.OriginalAlbum == songInfo.Album);

        return await Task.FromResult(mapped);
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

    public Task DeleteMappingAsync(MappedSongSearchQuery mapping)
    {
        var col = GetCollection();

        var target = col.FindOne(x =>
                x.OriginalTitle == mapping.OriginalTitle &&
                x.OriginalArtist == mapping.OriginalArtist &&
                x.OriginalAlbum == mapping.OriginalAlbum);

        if (target != null)
        {
            col.Delete(target.Id);
            _logger.LogDebug("Deleted song search mapping: {OriginalTitle} - {OriginalArtist}.",
                mapping.OriginalTitle, mapping.OriginalArtist);
        }

        return Task.CompletedTask;
    }
}