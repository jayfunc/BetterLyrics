using SQLite;

namespace BetterLyrics.WinUI3.Services.Database {
    public interface IDatabaseService {
        SQLiteConnection GetConnection();
    }
}
