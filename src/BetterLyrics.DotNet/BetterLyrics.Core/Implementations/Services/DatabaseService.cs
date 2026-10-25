using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class DatabaseService : IDatabaseService, IDisposable
{
    private readonly ILogger<DatabaseService> _logger;

    private LiteDatabase? _playHistoryDb;
    private LiteDatabase? _filesIndexDb;
    private LiteDatabase? _lyricsCacheDb;
    private LiteDatabase? _songSearchMapDb;
    private LiteDatabase? _playbackStateDb;

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
    }

    public LiteDatabase PlayHistoryDb => GetOrCreate(ref _playHistoryDb, PathHelper.PlayHistoryLiteDbPath);
    public LiteDatabase FilesIndexDb => GetOrCreate(ref _filesIndexDb, PathHelper.FilesIndexLiteDbPath);
    public LiteDatabase LyricsCacheDb => GetOrCreate(ref _lyricsCacheDb, PathHelper.LyricsCacheLiteDbPath);
    public LiteDatabase SongSearchMapDb => GetOrCreate(ref _songSearchMapDb, PathHelper.SongSearchMapLiteDbPath);
    public LiteDatabase PlaybackStateDb => GetOrCreate(ref _playbackStateDb, PathHelper.PlaybackStateLiteDbPath);

    public void Dispose()
    {
        // 置空以确保幂等：导入设置时会手动释放一次，退出时容器还会再释放一次
        DisposeDb(ref _playHistoryDb);
        DisposeDb(ref _filesIndexDb);
        DisposeDb(ref _lyricsCacheDb);
        DisposeDb(ref _songSearchMapDb);
        DisposeDb(ref _playbackStateDb);
    }

    private LiteDatabase GetOrCreate(ref LiteDatabase? database, string path)
    {
        if (database != null) return database;

        _logger.LogDebug("Opening LiteDB database: {Path}", path);
        try
        {
            database = new LiteDatabase(path);
            return database;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open LiteDB database: {Path}", path);
            throw;
        }
    }

    private void DisposeDb(ref LiteDatabase? db)
    {
        if (db == null) return;

        _logger.LogDebug("Closing LiteDB database.");
        db.Dispose();
        db = null;
    }
}
