using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SMTCService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MusicGalleryPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<DateTime?>>,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<PlaybackOrder>>
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IFileSystemService _fileSystemService;

        [ObservableProperty] public partial ISMTCService SMTCService { get; set; }

        private readonly DispatcherQueueTimer? _refreshSongsTimer;

        // All songs
        private List<ExtendedTrack> _allTracks = [];
        // Songs in current playlist or songs in current file tree
        private List<ExtendedTrack> _middleTracks = [];
        // Filtered songs based on search query for current playlist
        private List<ExtendedTrack> _filteredTracks = [];
        // Sorted songs based on filtered songs
        private List<ExtendedTrack> _sortedTracks = [];

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        [ObservableProperty] public partial bool IsLocalMediaNotFound { get; set; }

        /// <summary>
        /// Grouped tracks after filtering and sorting for current playlist
        /// </summary>
        [ObservableProperty] public partial ObservableCollection<GroupInfoList> GroupedTracks { get; set; } = [];

        [ObservableProperty] public partial List<ExtendedTrack> SelectedTracks { get; set; } = [];
        [ObservableProperty] public partial ExtendedTrack? SelectedFirstTrack { get; set; }

        [ObservableProperty] public partial int SelectedTracksTotalDuration { get; set; } = 0;

        [ObservableProperty] public partial CommonSongProperty SongOrderType { get; set; } = CommonSongProperty.Title;

        [ObservableProperty] public partial int SelectedSongsTabInfoIndex { get; set; } = 0;

        public SongsTabInfo? SelectedSongsTabInfo => AppSettings.StarredPlaylists.ElementAtOrDefault(SelectedSongsTabInfoIndex);

        [ObservableProperty] public partial bool IsDataSyncing { get; set; } = false;
        [ObservableProperty] public partial bool IsDataSyncError { get; set; } = false;

        [ObservableProperty] public partial string SongSearchQuery { get; set; } = string.Empty;

        [ObservableProperty] public partial ListViewSelectionMode SongListViewSelectionMode { get; set; } = ListViewSelectionMode.Single;

        public ObservableCollection<FolderNode> FolderRoots { get; } = new();

        public MusicGalleryPageViewModel(
            ISettingsService settingsService,
            ILocalizationService localizationService,
            IFileSystemService fileSystemService,
            ISMTCService smtcService
        )
        {
            _localizationService = localizationService;
            _fileSystemService = fileSystemService;
            SMTCService = smtcService;

            _refreshSongsTimer = DispatcherQueueHelper.Instance?.CreateTimer();

            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;

            RefreshSongs(true, true);

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
            _settingsService.AppSettings.LocalMediaFolders.ItemPropertyChanged += LocalMediaFolders_ItemPropertyChanged;
        }

        private void LocalMediaFolders_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            IsDataSyncError = AppSettings.LocalMediaFolders.Any(x => x.StatusSeverity == InfoBarSeverity.Error);
        }

        private void LocalMediaFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshSongs(true);
        }

        public void CancelRefreshSongs()
        {
        }

        public void RefreshSongs(bool recoverPlaybackPosition = false, bool allowAutoPlay = false)
        {
            _refreshSongsTimer?.Debounce(() =>
            {
                _ = Task.Run(async () =>
                {
                    var enabledFolderIds = _settingsService.AppSettings.LocalMediaFolders
                        .Where(f => f.IsEnabled)
                        .Select(f => f.Id)
                        .ToList();
                    var cachedFiles = await _fileSystemService.GetParsedFilesAsync(enabledFolderIds);
                    cachedFiles = cachedFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

                    var newTrackList = cachedFiles
                        .Select(x => new ExtendedTrack(x))
                        .ToList();
                    var sourceDict = newTrackList.ToDictionary(s => s.Uri, s => s);

                    var playQueue = _settingsService.AppSettings.MusicGallerySettings.PlayQueuePaths
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x =>
                        {
                            var encodedUri = new Uri(x).AbsoluteUri;
                            if (sourceDict.TryGetValue(encodedUri, out var found))
                            {
                                return new PlayQueueItem(found);
                            }
                            else
                            {
                                return null;
                            }
                        })
                        .Where(x => x != null)
                        .ToList();

                    DispatcherQueueHelper.Instance?.TryEnqueue(async () =>
                    {
                        _allTracks = newTrackList;

                        // 更新文件夹树
                        RefreshTreeView();

                        // 应用过滤器
                        ApplyPlaylist();
                        ApplySongSearchQuery();

                        IsLocalMediaNotFound = !_filteredTracks.Any();

                        ApplySongOrderType();

                        await SMTCService.UpdatePlaybackListAsync(playQueue, recoverPlaybackPosition, allowAutoPlay);
                    });
                });
            }, Time.DebounceTimeout);
        }

        public void ApplyPlaylist()
        {
            if (SelectedSongsTabInfo?.FilterValue == string.Empty)
            {
                _middleTracks = _allTracks;
            }
            else
            {
                switch (SelectedSongsTabInfo?.FilterProperty)
                {
                    case CommonSongProperty.Title:
                        _middleTracks = _allTracks.Where(t => t.Title.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Album:
                        _middleTracks = _allTracks.Where(t => t.Album.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Artist:
                        _middleTracks = _allTracks.Where(t => t.Artist.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.Folder:
                        _middleTracks = _allTracks.Where(t => t.ParentFolderPath.Equals(SelectedSongsTabInfo.FilterValue, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                    case CommonSongProperty.M3UFilePath:
                        if (SelectedSongsTabInfo.FilterValue is string path)
                        {
                            if (File.Exists(path))
                            {
                                var m3uFileContent = File.ReadAllText(path);
                                _middleTracks = _allTracks.Where(t => m3uFileContent.Contains(t.Uri.ToDecodedAbsoluteUri())).ToList();
                            }
                            else
                            {
                                _middleTracks = [];
                                GlobalToastManager.Show("PlaylistViewFailed", path, InfoBarSeverity.Success);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            ApplySongSearchQuery();
            IsLocalMediaNotFound = !_filteredTracks.Any();
            ApplySongOrderType();
        }

        public void ApplySongSearchQuery()
        {
            if (string.IsNullOrWhiteSpace(SongSearchQuery))
            {
                _filteredTracks = _middleTracks;
                return;
            }
            _filteredTracks = _middleTracks.Where(t =>
                    t.Title.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.Artist.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.Album.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.FileName.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    t.ParentFolderPath.Contains(SongSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void ApplySongOrderType()
        {
            switch (SongOrderType)
            {
                case CommonSongProperty.Title:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Title),
                        o => ((ExtendedTrack)o).Title
                    );
                    break;
                case CommonSongProperty.Artist:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Artist),
                        o => ((ExtendedTrack)o).Artist
                    );
                    break;
                case CommonSongProperty.Album:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.Album),
                        o => ((ExtendedTrack)o).Album
                    );
                    break;
                case CommonSongProperty.Folder:
                    GroupedTracks = _filteredTracks.GetGroupedBy(
                        t => LanguageHelper.GetOrderChar(t.ParentFolderName),
                        o => ((ExtendedTrack)o).Album
                    );
                    break;
            }
            _sortedTracks = GroupedTracks.SelectMany(x => x.Cast<ExtendedTrack>()).ToList();
        }

        private void RefreshTreeView()
        {
            var roots = FolderTreeBuilder.Build(_allTracks, AppSettings.LocalMediaFolders.ToList());

            FolderRoots.Clear();
            foreach (var r in roots) FolderRoots.Add(r);
        }

        public void SelectFolder(FolderNode? folder)
        {
            if (folder == null) return;
            if (_allTracks == null) return;

            string baseUri = folder.FolderPath;
            if (!baseUri.EndsWith("/")) baseUri += "/";
            string decodedBaseUri = System.Net.WebUtility.UrlDecode(baseUri);

            _middleTracks = _allTracks.Where(track =>
            {
                if (track.MediaFolderId != folder.MediaFolderId) return false;

                string decodedTrackUri = System.Net.WebUtility.UrlDecode(track.Uri);

                if (!decodedTrackUri.StartsWith(decodedBaseUri, StringComparison.OrdinalIgnoreCase)) return false;

                string relativePart = decodedTrackUri.Substring(decodedBaseUri.Length);

                return !relativePart.Contains('/');
            }).ToList();

            ApplySongSearchQuery();
            IsLocalMediaNotFound = !_filteredTracks.Any();
            ApplySongOrderType();
        }

        public void AddToPlaylists(SongsTabInfo playlist)
        {
            var starredPlaylists = AppSettings.StarredPlaylists;
            var found = starredPlaylists.FirstOrDefault(x => x.FilterProperty == playlist.FilterProperty && x.FilterValue == playlist.FilterValue);
            if (found == null)
            {
                starredPlaylists.Add(playlist);
                SelectedSongsTabInfoIndex = starredPlaylists.Count - 1;
            }
            else
            {
                SelectedSongsTabInfoIndex = starredPlaylists.IndexOf(found);
            }
            ApplyPlaylist();
        }

        partial void OnSongOrderTypeChanged(CommonSongProperty value)
        {
            ApplySongOrderType();
            IsLocalMediaNotFound = !_filteredTracks.Any();
        }

        partial void OnSongSearchQueryChanged(string value)
        {
            ApplySongSearchQuery();
            IsLocalMediaNotFound = !_filteredTracks.Any();
            ApplySongOrderType();
        }

        private void AddFileToStarredPlaylists(StorageFile file)
        {
            AppSettings.StarredPlaylists.Add(new SongsTabInfo
            {
                FilterProperty = CommonSongProperty.M3UFilePath,
                FilterValue = file.Path,
                Icon = "\uE7BC",
                Name = file.Name
            });
        }

        [RelayCommand]
        private async Task ShuffleAsync()
        {
            AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.Shuffle;

            var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
            await SMTCService.UpdatePlaybackListAsync(playQueue);

            int queueCount = playQueue.Count();
            int startIndex = queueCount > 0 ? Random.Shared.Next(0, queueCount) : -1;

            SMTCService.PlayTrackAt(startIndex);
        }

        [RelayCommand]
        private async Task RepeatAllAsync()
        {
            AppSettings.MusicGallerySettings.PlaybackOrder = PlaybackOrder.RepeatAll;

            var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
            await SMTCService.UpdatePlaybackListAsync(playQueue);

            SMTCService.PlayTrackAt(0);
        }

        [RelayCommand]
        private async Task PlayAsync(ExtendedTrack invokedTrack)
        {
            var playQueue = _sortedTracks.Select(x => new PlayQueueItem(x));
            await SMTCService.UpdatePlaybackListAsync(playQueue);

            var target = SMTCService.TrackPlayingQueue.FirstOrDefault(x => x.Track == invokedTrack);
            if (target != null)
            {
                int index = SMTCService.TrackPlayingQueue.IndexOf(target);
                if (index != -1)
                {
                    SMTCService.PlayTrackAt(index);
                }
            }
        }

        [RelayCommand]
        private async Task CreatePlaylistAsync()
        {
            var file = await PickerHelper.PickSaveFileAsync<MusicGalleryWindow>(new Dictionary<string, IList<string>>()
            {
                { "M3U", [".m3u"] }
            });

            if (file != null)
            {
                AddFileToStarredPlaylists(file);
                GlobalToastManager.Show("CreatePlaylistSuccessfully", file.Path, InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private async Task ImportPlaylistAsync()
        {
            var file = await PickerHelper.PickSingleFileAsync<MusicGalleryWindow>([".m3u"]);

            if (file != null)
            {
                AddFileToStarredPlaylists(file);
                GlobalToastManager.Show("ImportPlaylistSuccessfully", file.Path, InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private void StopTrack()
        {
            SMTCService.PlayTrackAt(-1);
        }

        [RelayCommand]
        private void OpenMediaSettings()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
            var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
            settingsPageViewModel.NavigateToSection(SettingsSection.MediaLib);
        }

        [RelayCommand]
        private void ToggleSongListViewSelectionMode()
        {
            SongListViewSelectionMode =
                SongListViewSelectionMode == ListViewSelectionMode.Single ?
                ListViewSelectionMode.Multiple :
                ListViewSelectionMode.Single;
        }

        public void Receive(PropertyChangedMessage<DateTime?> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.LastSyncTime))
                {
                    RefreshSongs(true);
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                {
                    RefreshSongs(true);
                }
                else if (message.PropertyName == nameof(MediaFolder.IsProcessing))
                {
                    IsDataSyncing = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.Name))
                {
                    RefreshTreeView();
                }
            }
        }

        public void Receive(PropertyChangedMessage<PlaybackOrder> message)
        {
            if (message.Sender is MusicGallerySettings)
            {
                if (message.PropertyName == nameof(MusicGallerySettings.PlaybackOrder))
                {
                    SMTCService.ApplyPlaybackOrder(message.NewValue);
                }
            }
        }

    }
}
