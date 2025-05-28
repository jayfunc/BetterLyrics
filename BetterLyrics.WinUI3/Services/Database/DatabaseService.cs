using BetterLyrics.WinUI3.Models;
using SQLite;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Services.Database {
    public class DatabaseService : IDatabaseService {
        private readonly SQLiteConnection _connection;

        public DatabaseService() {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MusicMetadataIndex.db"
            );
            _connection = new SQLiteConnection(dbPath);
            _connection.CreateTable<MetadataIndex>();
        }

        public SQLiteConnection GetConnection() {
            return _connection;
        }
    }
}
