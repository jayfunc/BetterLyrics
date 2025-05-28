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
using Windows.UI;

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
        private Color _startGraidentColor;

        [ObservableProperty]
        private Color _endGraidentColor;

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
            List<string> localMusicFolderPaths = [.. JsonSerializer.Deserialize<List<string>>(_settingsService.Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries))];

            var db = _databaseService.GetConnection();
            db.DeleteAll<MetadataIndex>();

            foreach (var musicFolderPath in localMusicFolderPaths) {
                foreach (var file in Directory.GetFiles(musicFolderPath)) {
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
            for (int i = 0; i < lyricsPhrases.Count; i++) {
                var lyricsPhrase = lyricsPhrases[i];
                int lyricsPhraseStartTimestampMs = lyricsPhrase.TimestampMs;
                int lyricsPhraseEndTimestampMs = 0;

                if (i + 1 < lyricsPhrases.Count) {
                    lyricsPhraseEndTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                } else {
                    lyricsPhraseEndTimestampMs = (int)track.DurationMs;
                }

                var lyricsLine = new LyricsLine {
                    StartTimestampMs = lyricsPhraseStartTimestampMs,
                    EndTimestampMs = lyricsPhraseEndTimestampMs,
                    IsPlaying = false,
                    Text = lyricsPhrase.Text,
                };
                lyricsLine.DurationMs = lyricsLine.EndTimestampMs - lyricsLine.StartTimestampMs;
                lyricsLine.AverageDurationPerCharMs = lyricsLine.DurationMs / lyricsLine.Text.Length;

                List<LyricsLineChar> lyricsLineChars = [];
                var lyricsLineCharStartTimestampMs = lyricsPhraseStartTimestampMs;

                foreach (var ch in lyricsLine.Text) {

                    var lyricsLineChar = new LyricsLineChar {
                        Text = (ch == ' ' ? (char)160 : ch).ToString(),
                        StartTimestampMs = lyricsLineCharStartTimestampMs,
                    };
                    lyricsLineChar.EndTimestampMs = lyricsLineChar.StartTimestampMs + lyricsLine.AverageDurationPerCharMs;
                    lyricsLineChar.DurationMs = lyricsLineChar.EndTimestampMs - lyricsLineChar.StartTimestampMs;
                    lyricsLineChars.Add(lyricsLineChar);

                    lyricsLineCharStartTimestampMs += lyricsLine.AverageDurationPerCharMs;
                }

                lyricsLine.LyricsLineChars = [.. lyricsLineChars];
                result.Add(lyricsLine);
            }
            return result;

        }

    }
}
