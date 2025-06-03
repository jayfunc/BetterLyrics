using BetterLyrics.WinUI3.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper {
    public class DatabaseHelper {
        private static SQLiteConnection _database;

        public static SQLiteConnection InitializeDatabase() {
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicMetadataIndex.db");
            _database = new SQLiteConnection(dbPath);
            _database.CreateTable<MetadataIndex>(); // Create table if it doesn't exist
            return _database;
        }
    }
}
