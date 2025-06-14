﻿using System;
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
using Windows.Media.Control;

namespace BetterLyrics.WinUI3.Services.Database
{
    public class DatabaseService
    {
        private readonly SQLiteConnection _connection;

        private readonly CharsetDetector _charsetDetector = new();

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

        public Track? GetMusicMetadata(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            if (mediaProps == null || mediaProps.Title == null || mediaProps.Artist == null)
                return null;

            var founds = _connection
                .Table<MetadataIndex>()
                // Look up by Title and Artist (these two props were fetched by reading metadata in music file befoe) first
                // then by Path (music file name usually contains song name and artist so this can be a second way to look up for)
                // Please note for .lrc file, only the second way works for it
                .Where(m =>
                    (
                        m.Title != null
                        && m.Artist != null
                        && m.Title.Contains(mediaProps.Title)
                        && m.Artist.Contains(mediaProps.Artist)
                    )
                    || (
                        m.Path != null
                        && m.Path.Contains(mediaProps.Title)
                        && m.Path.Contains(mediaProps.Artist)
                    )
                )
                .ToList();
            if (founds == null || founds.Count == 0)
            {
                return null;
            }
            else
            {
                var first = new Track(founds[0].Path);
                if (founds.Count == 1)
                {
                    return first;
                }
                else
                {
                    if (first.Lyrics.Exists())
                    {
                        return first;
                    }
                    else
                    {
                        foreach (var found in founds)
                        {
                            if (found.Path.EndsWith(".lrc"))
                            {
                                using (FileStream fs = File.OpenRead(found.Path))
                                {
                                    _charsetDetector.Feed(fs);
                                    _charsetDetector.DataEnd();
                                }

                                string content;
                                if (_charsetDetector.Charset != null)
                                {
                                    Encoding encoding = Encoding.GetEncoding(
                                        _charsetDetector.Charset
                                    );
                                    content = File.ReadAllText(found.Path, encoding);
                                }
                                else
                                {
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
