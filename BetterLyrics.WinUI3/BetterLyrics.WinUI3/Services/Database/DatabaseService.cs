using ATL;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ude;

namespace BetterLyrics.WinUI3.Services.Database {
    public class DatabaseService {

        private readonly SQLiteConnection _connection;
        private readonly CharsetDetector _charsetDetector = new();

        public DatabaseService() {
            _connection = new SQLiteConnection(Helper.AppInfo.DatabasePath);
            if (_connection.GetTableInfo("MetadataIndex").Count == 0) {
                _connection.CreateTable<MetadataIndex>();
            }
        }

        public async Task RebuildMusicMetadataIndexDatabaseAsync(IList<string> paths) {
            await Task.Run(() => {
                _connection.DeleteAll<MetadataIndex>();
                foreach (var path in paths) {
                    if (Directory.Exists(path)) {
                        foreach (var file in Directory.GetFiles(path)) {
                            var fileExtension = Path.GetExtension(file);
                            var track = new Track(file);
                            _connection.Insert(new MetadataIndex {
                                Path = file,
                                Title = track.Title,
                                Artist = track.Artist,
                            });
                        }
                    }
                }
            });
        }

        public Track? GetMusicMetadata(string? title, string? artist) {
            var founds = _connection.Table<MetadataIndex>()
                .Where(m => m.Title == title && m.Artist == artist).ToList();
            if (founds == null || founds.Count == 0) {
                return null;
            } else {
                var first = new Track(founds[0].Path);
                if (founds.Count == 1) {
                    return first;
                } else {
                    if (first.Lyrics.Exists()) {
                        return first;
                    } else {
                        foreach (var found in founds) {
                            if (found.Path.EndsWith(".lrc")) {
                                using (FileStream fs = File.OpenRead(found.Path)) {
                                    _charsetDetector.Feed(fs);
                                    _charsetDetector.DataEnd();
                                }

                                string content;
                                if (_charsetDetector.Charset != null) {
                                    Encoding encoding = Encoding.GetEncoding(_charsetDetector.Charset);
                                    content = File.ReadAllText(found.Path, encoding);
                                } else {
                                    content = File.ReadAllText(found.Path, Encoding.UTF8);
                                }
                                first.Lyrics.ParseLRC(content);

                                return first;
                            }
                        }
                        return first;
                    }
                }
            }
        }


    }
}
