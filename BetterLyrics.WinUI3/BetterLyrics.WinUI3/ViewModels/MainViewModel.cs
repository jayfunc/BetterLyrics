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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI;
using WinUIEx;
using SystemBackdrop = Microsoft.UI.Xaml.Media.SystemBackdrop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private BitmapImage? _coverImage;

        [ObservableProperty]
        private SongInfo? _songInfo = null;

        private DisplayType? _preferredDisplayType = DisplayType.SplitView;

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        [ObservableProperty]
        private bool _isPlaying = false;

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

        private bool _isDesktopMode = false;
        public bool IsDesktopMode
        {
            get => _isDesktopMode;
            set
            {
                _isDesktopMode = value;
                OnPropertyChanged();
                WeakReferenceMessenger.Default.Send(new IsDesktopModeChangedMessage(value));
            }
        }

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainViewModel(IPlaybackService playbackService)
        {
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

            DisplayType displayType;

            if (songInfo == null)
            {
                displayType = DisplayType.PlaceholderOnly;
            }
            else if (_preferredDisplayType is DisplayType preferredDisplayType)
            {
                displayType = preferredDisplayType;
            }
            else
            {
                displayType = DisplayType.SplitView;
            }

            WeakReferenceMessenger.Default.Send(new DisplayTypeChangedMessage(displayType));

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
            _preferredDisplayType = (DisplayType)index;
            WeakReferenceMessenger.Default.Send(new DisplayTypeChangedMessage((DisplayType)index));
        }
    }
}
