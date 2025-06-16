using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI;
using SQLite;
using Ude;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.Database
{
    public class DatabaseService : IDatabaseService
    {
        private readonly SQLiteConnection _connection;

        private readonly CharsetDetector _charsetDetector = new();

        public DatabaseService()
        {
            _connection = new SQLiteConnection(AppInfo.DatabasePath);
            if (_connection.GetTableInfo("MetadataIndex").Count == 0)
            {
                _connection.CreateTable<MetadataIndex>();
            }
        }

        public async Task RebuildDatabaseAsync(IList<string> paths)
        {
            await Task.Run(() =>
            {
                _connection.DeleteAll<MetadataIndex>();

                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (
                            var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        )
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

        public async Task<SongInfo> FindSongInfoAsync(
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProps
        )
        {
            if (mediaProps == null || mediaProps.Title == null || mediaProps.Artist == null)
                return new();

            var songInfo = new SongInfo { Title = mediaProps?.Title, Artist = mediaProps?.Artist };

            // App.ResourceLoader!.GetString("MainPageNoLocalFilesMatched");

            if (mediaProps?.Thumbnail is IRandomAccessStreamReference streamReference)
            {
                songInfo.AlbumArt = await ImageHelper.ToByteArrayAsync(streamReference);
            }

            return await FindSongInfoAsync(songInfo, mediaProps!.Title, mediaProps!.Artist);
        }

        public async Task<SongInfo> FindSongInfoAsync(
            SongInfo initSongInfo,
            string searchTitle,
            string searchArtist
        )
        {
            var founds = _connection
                .Table<MetadataIndex>()
                // Look up by Title and Artist (these two props were fetched by reading metadata in music file befoe) first
                // then by Path (music file name usually contains song name and artist so this can be a second way to look up for)
                // Please note for .lrc file, only the second way works for it
                .Where(m =>
                    (
                        m.Title != null
                        && m.Artist != null
                        && m.Title.Contains(searchTitle)
                        && m.Artist.Contains(searchArtist)
                    )
                    || (
                        m.Path != null
                        && m.Path.Contains(searchTitle)
                        && m.Path.Contains(searchArtist)
                    )
                )
                .ToList();

            foreach (var found in founds)
            {
                initSongInfo.FilesFound ??= [];
                initSongInfo.FilesFound.Add(found.Path!);
                if (initSongInfo.LyricsLines == null || initSongInfo.AlbumArt == null)
                {
                    Track track = new(found.Path);
                    initSongInfo.ParseLyrics(track);
                    // Successfully parse lyrics info from metadata in music file
                    if (initSongInfo.LyricsLines != null)
                    {
                        // Used as lyrics source
                    }
                    // Find lyrics file
                    if (initSongInfo.LyricsLines == null && found?.Path?.EndsWith(".lrc") == true)
                    {
                        using (FileStream fs = File.OpenRead(found.Path))
                        {
                            _charsetDetector.Feed(fs);
                            _charsetDetector.DataEnd();
                        }

                        string content;
                        if (_charsetDetector.Charset != null)
                        {
                            Encoding encoding = Encoding.GetEncoding(_charsetDetector.Charset);
                            content = File.ReadAllText(found.Path, encoding);
                        }
                        else
                        {
                            content = File.ReadAllText(found.Path, Encoding.UTF8);
                        }
                        initSongInfo.ParseLyrics(track, content);
                        // Used as lyrics source
                    }

                    // Finf album art
                    if (initSongInfo.AlbumArt == null)
                    {
                        if (track.EmbeddedPictures.Count > 0)
                        {
                            initSongInfo.AlbumArt = track.EmbeddedPictures[0].PictureData;
                            // Used as album art source
                        }
                    }
                }
                else
                    break;
            }

            if (initSongInfo.AlbumArt == null)
            {
                initSongInfo.CoverImageDominantColors =
                [
                    .. Enumerable.Repeat(Colors.Transparent, ImageHelper.AccentColorCount),
                ];
            }
            else
            {
                initSongInfo.CoverImageDominantColors =
                [
                    .. await ImageHelper.GetAccentColorsFromByte(initSongInfo.AlbumArt),
                ];
            }

            return initSongInfo;
        }
    }
}
