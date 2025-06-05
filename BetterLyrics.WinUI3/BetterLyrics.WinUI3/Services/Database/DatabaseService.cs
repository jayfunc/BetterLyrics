using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using SQLite;
using Ude;

namespace BetterLyrics.WinUI3.Services.Database
{
    public class DatabaseService
    {
        private readonly SQLiteConnection _connection;

        public DatabaseService()
        {
            _connection = new SQLiteConnection(Helper.AppInfo.DatabasePath);
            if (_connection.GetTableInfo("MetadataIndex").Count == 0)
            {
                _connection.CreateTable<MetadataIndex>();
            }
        }

        public async Task RebuildMusicMetadataIndexDatabaseAsync(IList<string> paths)
        {
            await Task.Run(() =>
            {
                _connection.DeleteAll<MetadataIndex>();
                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var file in Directory.GetFiles(path))
                        {
                            var fileExtension = Path.GetExtension(file);
                            var track = new Track(file);
                            _connection.Insert(
                                new MetadataIndex
                                {
                                    Path = file,
                                    Title = track.Title,
                                    Artist = track.Artist,
                                }
                            );
                        }
                    }
                }
            });
        }
    }
}
