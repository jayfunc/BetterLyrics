using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.SongSearchMapService
{
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

        public async Task<MappedSongSearchQuery?> GetMappingAsync(string title, string artist, string album)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.SongSearchMap
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.OriginalTitle == title &&
                    x.OriginalArtist == artist &&
                    x.OriginalAlbum == album);
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
}
