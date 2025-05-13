using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Control;
using ATL;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DispatcherQueue _dispatcherQueue;

        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager = null;
        private GlobalSystemMediaTransportControlsSession? _currentSession = null;

        /// <summary>
        /// Current lyrics line index
        /// </summary>
        [ObservableProperty]
        private int _lyricsLineIndex = 0;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _artist;

        [ObservableProperty]
        private ObservableCollection<LyricsLine> _lyricsLines = new();

        [ObservableProperty]
        private BitmapImage _coverBitmapImage = new();

        private List<String> _localMusicFolderPaths =
        [
            "D:/Musics"
        ];

        public MainViewModel(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            InitMediaManager();
        }

        private async void InitMediaManager()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;

            if (_sessionManager.GetCurrentSession() != null)
            {
                _currentSession = _sessionManager.GetCurrentSession();
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
                CurrentSession_MediaPropertiesChanged(_currentSession, null);
                CurrentSession_TimelinePropertiesChanged(_currentSession, null);
            }
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            // Unregister events associated with the previous session  
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
            }

            // Record and register events for current session
            _currentSession = sender.GetCurrentSession();
            _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
            _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            var currentPositionMs = sender.GetTimelineProperties().Position.TotalMilliseconds;

            for (int i = 0; i < LyricsLines.Count; i++)
            {
                var lyricsLine = LyricsLines[i];

                if (lyricsLine.StartTimestampMs <= currentPositionMs &&
                    lyricsLine.EndTimestampMs >= currentPositionMs)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                    {
                        if (LyricsLineIndex != i)
                        {
                            // Unhighlight previous line if it is not the same as current line
                            LyricsLines[LyricsLineIndex].IsPlaying = false;
                            LyricsLines[LyricsLineIndex].PlayedProgress = 1;

                            LyricsLineIndex = i;

                            // Highlight current line
                            LyricsLines[LyricsLineIndex].IsPlaying = true;
                            LyricsLines[LyricsLineIndex].PlayedProgress = 0;
                        }
                        else
                        {
                            LyricsLines[LyricsLineIndex].PlayedProgress =
                                (currentPositionMs - LyricsLines[LyricsLineIndex].StartTimestampMs) / LyricsLines[LyricsLineIndex].DurationMs;
                        }
                    });

                    break;
                }
            }
        }

        /// <summary>
        /// Note: this func is invoked by non-UI thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            var mediaProps = await sender.TryGetMediaPropertiesAsync();

            if (mediaProps == null)
            {
                return;
            }

            var artist = mediaProps?.Artist;
            var title = mediaProps?.Title;
            var coverImgStreamFromMediaProps = mediaProps?.Thumbnail;

            ObservableCollection<LyricsLine> lyricsLines = new();
            byte[]? imgByteFromTag = null;

            foreach (var musicFolderPath in _localMusicFolderPaths)
            {
                foreach (var file in Directory.GetFiles(musicFolderPath))
                {
                    var fileExtension = Path.GetExtension(file);
                    if (fileExtension != ".mp3" && fileExtension != ".flac")
                    {
                        continue;
                    }
                    var track = new Track(file);

                    if (track.Title == mediaProps.Title && track.Artist == mediaProps.Artist)
                    {
                        // Get picture from tag
                        imgByteFromTag = track.EmbeddedPictures[0].PictureData;

                        // Get lyrics
                        var lyricsPhrases = track.Lyrics.SynchronizedLyrics;
                        for (int i = 0; i < lyricsPhrases.Count; i++)
                        {
                            var lyricsPhrase = lyricsPhrases[i];
                            int endTimestampMs = 0;

                            if (i + 1 < lyricsPhrases.Count)
                            {
                                endTimestampMs = lyricsPhrases[i + 1].TimestampMs;
                            }
                            else
                            {
                                endTimestampMs = (int)track.DurationMs;
                            }

                            lyricsLines.Add(new LyricsLine
                            {
                                StartTimestampMs = lyricsPhrase.TimestampMs,
                                EndTimestampMs = endTimestampMs,
                                Text = lyricsPhrase.Text,
                                IsPlaying = false,
                            });

                        }
                    }
                }
            }

            _dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                // Set cover image
                if (coverImgStreamFromMediaProps == null)
                {
                    if (imgByteFromTag == null)
                    {
                        CoverBitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/Images/Cover.jpg"));
                    }
                    else
                    {
                        CoverBitmapImage = await Helper.Bitmap.LoadBitmapImageFromBytesAsync(imgByteFromTag);
                    }
                }
                else
                {
                    var stream = await coverImgStreamFromMediaProps.OpenReadAsync();
                    await CoverBitmapImage.SetSourceAsync(stream);
                }

                // Set song info
                Title = title;
                Artist = artist;

                // Empty lyrics lines
                if (lyricsLines == null)
                {
                    LyricsLines.Clear();
                }
                else
                {
                    LyricsLines = lyricsLines;
                }
                LyricsLineIndex = 0;
            });
        }

    }
}