using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Entities;
using Dapper;
using LiteDB;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

public class DatabaseMigrationService : Interfaces.Services.IDatabaseMigrationService
{
    private readonly Interfaces.Services.IDatabaseService _databaseService;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(Interfaces.Services.IDatabaseService databaseService, ILogger<DatabaseMigrationService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task MigrateAllAsync()
    {
        try
        {
            await MigratePlayHistoryAsync();
            await MigrateFilesIndexAsync();
            await MigrateLyricsCacheAsync();
            await MigrateSongSearchMapAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate databases from SQLite to LiteDB.");
        }
    }

    private async Task<string?> GetTableNameAsync(SqliteConnection connection)
    {
        return await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrationsHistory' LIMIT 1");
    }

    private async Task MigratePlayHistoryAsync()
    {
        var dbPath = PathHelper.PlayHistoryPath;
        var liteDbPath = PathHelper.PlayHistoryLiteDbPath;
        if (!File.Exists(dbPath) || File.Exists(liteDbPath)) return;

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await connection.OpenAsync();

                var tableName = await GetTableNameAsync(connection);
                if (tableName != null)
                {
                    var items = await connection.QueryAsync<PlayHistoryItem>($"SELECT * FROM \"{tableName}\"");
                    if (items.Any())
                    {
                        var col = _databaseService.PlayHistoryDb.GetCollection<PlayHistoryItem>("playHistory");
                        foreach (var item in items)
                        {
                            item.Id = 0;
                        }
                        col.InsertBulk(items);
                    }
                }

                await connection.CloseAsync();
            }

            SqliteConnection.ClearAllPools();
            File.Move(dbPath, dbPath + ".bak", true);
            _logger.LogInformation("PlayHistory migration completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating PlayHistory");
        }
    }

    private async Task MigrateFilesIndexAsync()
    {
        var dbPath = PathHelper.FilesIndexPath;
        var liteDbPath = PathHelper.FilesIndexLiteDbPath;
        if (!File.Exists(dbPath) || File.Exists(liteDbPath)) return;

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await connection.OpenAsync();

                var tableName = await GetTableNameAsync(connection);
                if (tableName != null)
                {
                    var items = await connection.QueryAsync<FilesIndexItem>($"SELECT * FROM \"{tableName}\"");
                    if (items.Any())
                    {
                        var col = _databaseService.FilesIndexDb.GetCollection<FilesIndexItem>("filesIndex");
                        foreach (var item in items)
                        {
                            item.Id = 0;
                        }
                        col.InsertBulk(items);
                    }
                }

                await connection.CloseAsync();
            }

            SqliteConnection.ClearAllPools();
            File.Move(dbPath, dbPath + ".bak", true);
            _logger.LogInformation("FilesIndex migration completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating FilesIndex");
        }
    }

    private async Task MigrateLyricsCacheAsync()
    {
        var dbPath = PathHelper.LyricsCachePath;
        var liteDbPath = PathHelper.LyricsCacheLiteDbPath;
        if (!File.Exists(dbPath) || File.Exists(liteDbPath)) return;

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await connection.OpenAsync();

                var tableName = await GetTableNameAsync(connection);
                if (tableName != null)
                {
                    var items = await connection.QueryAsync<LyricsCacheItem>($"SELECT * FROM \"{tableName}\"");
                    if (items.Any())
                    {
                        var col = _databaseService.LyricsCacheDb.GetCollection<LyricsCacheItem>("lyricsCache");
                        foreach (var item in items)
                        {
                            item.Id = 0;
                        }
                        col.InsertBulk(items);
                    }
                }

                await connection.CloseAsync();
            }

            SqliteConnection.ClearAllPools();
            File.Move(dbPath, dbPath + ".bak", true);
            _logger.LogInformation("LyricsCache migration completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating LyricsCache");
        }
    }

    private class LegacyMappedSongSearchQuery
    {
        public string Id { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public string OriginalArtist { get; set; } = string.Empty;
        public string OriginalAlbum { get; set; } = string.Empty;
        public string MappedTitle { get; set; } = string.Empty;
        public string MappedArtist { get; set; } = string.Empty;
        public string MappedAlbum { get; set; } = string.Empty;
        public bool IsMarkedAsPureMusic { get; set; }
        public int? LyricsSearchProvider { get; set; }
    }

    private async Task MigrateSongSearchMapAsync()
    {
        var dbPath = PathHelper.SongSearchMapPath;
        var liteDbPath = PathHelper.SongSearchMapLiteDbPath;
        if (!File.Exists(dbPath) || File.Exists(liteDbPath)) return;

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                await connection.OpenAsync();

                var tableName = await GetTableNameAsync(connection);
                if (tableName != null)
                {
                    var items = await connection.QueryAsync<LegacyMappedSongSearchQuery>($"SELECT * FROM \"{tableName}\"");
                    if (items.Any())
                    {
                        var col = _databaseService.SongSearchMapDb.GetCollection<MappedSongSearchQuery>("songSearchMap");

                        var newItems = items.Select(x => new MappedSongSearchQuery
                        {
                            Id = ObjectId.NewObjectId(),
                            OriginalTitle = x.OriginalTitle,
                            OriginalArtist = x.OriginalArtist,
                            OriginalAlbum = x.OriginalAlbum,
                            MappedTitle = x.MappedTitle,
                            MappedArtist = x.MappedArtist,
                            MappedAlbum = x.MappedAlbum,
                            IsMarkedAsPureMusic = x.IsMarkedAsPureMusic,
                            LyricsSearchProvider = x.LyricsSearchProvider.HasValue ? (Enums.LyricsProvider)x.LyricsSearchProvider.Value : null
                        });

                        col.InsertBulk(newItems);
                    }
                }

                await connection.CloseAsync();
            }

            SqliteConnection.ClearAllPools();
            File.Move(dbPath, dbPath + ".bak", true);
            _logger.LogInformation("SongSearchMap migration completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating SongSearchMap");
        }
    }
}
