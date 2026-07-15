using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using LiteDB;

namespace BetterLyrics.Core.Implementations.Services;

public class DatabaseService : IDatabaseService, IDisposable
{
    private LiteDatabase? _playHistoryDb;
    private LiteDatabase? _filesIndexDb;
    private LiteDatabase? _lyricsCacheDb;
    private LiteDatabase? _songSearchMapDb;

    public LiteDatabase PlayHistoryDb => _playHistoryDb ??= new LiteDatabase(PathHelper.PlayHistoryLiteDbPath);
    public LiteDatabase FilesIndexDb => _filesIndexDb ??= new LiteDatabase(PathHelper.FilesIndexLiteDbPath);
    public LiteDatabase LyricsCacheDb => _lyricsCacheDb ??= new LiteDatabase(PathHelper.LyricsCacheLiteDbPath);
    public LiteDatabase SongSearchMapDb => _songSearchMapDb ??= new LiteDatabase(PathHelper.SongSearchMapLiteDbPath);


    public void Dispose()
    {
        _playHistoryDb?.Dispose();
        _filesIndexDb?.Dispose();
        _lyricsCacheDb?.Dispose();
        _songSearchMapDb?.Dispose();
    }
}
