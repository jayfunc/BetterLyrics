using LiteDB;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IDatabaseService
{
    LiteDatabase PlayHistoryDb { get; }
    LiteDatabase FilesIndexDb { get; }
    LiteDatabase LyricsCacheDb { get; }
    LiteDatabase SongSearchMapDb { get; }
}
