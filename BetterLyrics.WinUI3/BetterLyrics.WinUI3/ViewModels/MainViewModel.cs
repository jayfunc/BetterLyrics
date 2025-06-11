using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<bool> _isDisplayTypeEnabled =
        [
            .. Enumerable.Repeat(false, Enum.GetValues<DisplayType>().Length),
        ];

        [ObservableProperty]
        private BitmapImage? _coverImage;

        [ObservableProperty]
        private SongInfo? _songInfo = null;

        [ObservableProperty]
        private int _displayType = (int)Models.DisplayType.PlaceholderOnly;

        private int? _preferredDisplayType = 2;

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private ObservableCollection<string> _matchedLocalFilePath = [];

        private bool _isImmersiveMode = false;
        public bool IsImmersiveMode
        {
            get => _isImmersiveMode;
            set
            {
                _isImmersiveMode = value;
                OnPropertyChanged();
                WeakReferenceMessenger.Default.Send(new IsImmersiveModeChangedMessage(value));
                if (value)
                    WeakReferenceMessenger.Default.Send(
                        new ShowNotificatonMessage(
                            new Notification(
                                App.ResourceLoader!.GetString("MainPageEnterImmersiveModeHint"),
                                isForeverDismissable: true,
                                relatedSettingsKeyName: SettingsKeys.NeverShowEnterImmersiveModeMessage
                            )
                        )
                    );
            }
        }

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private readonly IDatabaseService _databaseService;

        public MainViewModel(IPlaybackService playbackService, IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            WeakReferenceMessenger.Default.Register<MainViewModel, PlayingStatusChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () =>
                        {
                            IsPlaying = m.Value;
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<MainViewModel, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            await UpdateSongInfoUI(m.Value);
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<MainViewModel, ReFindSongInfoRequestedMessage>(
                this,
                async (r, m) =>
                {
                    if (SongInfo == null || SongInfo.Title == null || SongInfo.Artist == null)
                        return;

                    await UpdateSongInfoUI(
                        _databaseService.FindSongInfo(SongInfo, SongInfo.Title, SongInfo.Artist)
                    );
                }
            );
        }

        private async Task UpdateSongInfoUI(SongInfo? songInfo)
        {
            AboutToUpdateUI = true;
            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

            SongInfo = songInfo;

            await Task.Delay(1);

            CoverImage =
                (songInfo?.AlbumArt == null)
                    ? null
                    : await ImageHelper.GetBitmapImageFromBytesAsync(songInfo.AlbumArt);

            IsDisplayTypeEnabled =
            [
                .. Enumerable.Repeat(false, Enum.GetValues<DisplayType>().Length),
            ];

            if (songInfo == null)
            {
                IsDisplayTypeEnabled[(int)Models.DisplayType.PlaceholderOnly] = true;
                DisplayType = (int)Models.DisplayType.PlaceholderOnly;
            }
            else
            {
                if (songInfo.LyricsLines?.Count > 0)
                {
                    IsDisplayTypeEnabled[(int)Models.DisplayType.LyricsOnly] = true;
                }
                if (songInfo.AlbumArt != null)
                {
                    IsDisplayTypeEnabled[(int)Models.DisplayType.AlbumArtOnly] = true;
                }
                IsDisplayTypeEnabled[(int)Models.DisplayType.SplitView] =
                    IsDisplayTypeEnabled[(int)Models.DisplayType.LyricsOnly]
                    && IsDisplayTypeEnabled[(int)Models.DisplayType.AlbumArtOnly];

                // Set checked
                if (
                    IsDisplayTypeEnabled[(int)Models.DisplayType.SplitView]
                    && _preferredDisplayType == (int)Models.DisplayType.SplitView
                )
                {
                    DisplayType = (int)Models.DisplayType.SplitView;
                }
                else
                {
                    if (
                        IsDisplayTypeEnabled[(int)Models.DisplayType.LyricsOnly]
                        && _preferredDisplayType == (int)Models.DisplayType.LyricsOnly
                    )
                    {
                        DisplayType = (int)Models.DisplayType.LyricsOnly;
                    }
                    else if (
                        IsDisplayTypeEnabled[(int)Models.DisplayType.AlbumArtOnly]
                        && _preferredDisplayType == (int)Models.DisplayType.AlbumArtOnly
                    )
                    {
                        DisplayType = (int)Models.DisplayType.AlbumArtOnly;
                    }
                }
            }

            AboutToUpdateUI = false;
        }

        public void OpenMatchedFileFolderInFileExplorer(string path)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                }
            );
        }

        [RelayCommand]
        private void OnDisplayTypeChanged(object value)
        {
            int index = Convert.ToInt32(value);
            _preferredDisplayType = index;
            DisplayType = index;
        }
    }
}
