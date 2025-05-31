using ATL;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.System.UserProfile;
using Windows.UI;
using static ATL.LyricsInfo;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class MainViewModel : ObservableObject {
        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        [ObservableProperty]
        private ObservableCollection<Color> _coverImageDominantColors =
            [Colors.Transparent, Colors.Transparent, Colors.Transparent];

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        [ObservableProperty]
        private bool _isSmallScreenMode;

        private readonly IDatabaseService _databaseService;
        private readonly ISettingsService _settingsService;

        public MainViewModel(IDatabaseService databaseService, ISettingsService settingsService) {
            _databaseService = databaseService;
            _settingsService = settingsService;
        }

        public void RefreshMusicMetadataIndexDatabase() {
            List<MusicFolder> localMusicFolders = _settingsService.MusicLibraries;

            var db = _databaseService.GetConnection();
            db.DeleteAll<MetadataIndex>();

            foreach (var localMusicFolder in localMusicFolders) {
                if (localMusicFolder.IsValid) {
                    foreach (var file in Directory.GetFiles(localMusicFolder.Path)) {
                        var fileExtension = Path.GetExtension(file);
                        if (fileExtension != ".mp3" && fileExtension != ".flac") {
                            continue;
                        }
                        var track = new Track(file);
                        db.Insert(new MetadataIndex {
                            Path = file,
                            Title = track.Title,
                            Artist = track.Artist,
                        });
                    }
                }

            }

        }

        public Track? GetMusicMetadata(string? title, string? artist) {
            var db = _databaseService.GetConnection();
            var metadataIndex = db.Table<MetadataIndex>()
                .Where(m => m.Title == title && m.Artist == artist)
                .FirstOrDefault();
            if (metadataIndex != null) {
                return new Track(metadataIndex.Path);
            } else {
                return null;
            }
        }

        public List<LyricsLine> GetLyrics(Track track) {
            List<LyricsLine> result = [];
            var lyricsPhrases = track.Lyrics.SynchronizedLyrics;

            if (lyricsPhrases.Count > 0) {
                if (lyricsPhrases[0].TimestampMs > 0) {
                    var placeholder = new LyricsPhrase(0, " ");
                    lyricsPhrases.Insert(0, placeholder);
                    lyricsPhrases.Insert(0, placeholder);
                }
            }

            LyricsLine? lyricsLine = null;

            for (int i = 0; i < lyricsPhrases.Count; i++) {
                var lyricsPhrase = lyricsPhrases[i];
                int startTimestampMs = lyricsPhrase.TimestampMs;
                int endTimestampMs;

                if (i + 1 < lyricsPhrases.Count) {
                    endTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                } else {
                    endTimestampMs = (int)track.DurationMs;
                }

                lyricsLine ??= new LyricsLine {
                    StartTimestampMs = startTimestampMs,
                };

                lyricsLine.Texts.Add(lyricsPhrase.Text);

                if (endTimestampMs == startTimestampMs) {
                    continue;
                } else {
                    lyricsLine.EndTimestampMs = endTimestampMs;
                    result.Add(lyricsLine);
                    lyricsLine = null;
                }

            }
            return result;

        }

    }
}
