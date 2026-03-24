using BetterLyrics.WinUI3.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AppSettings : ObservableRecipient
    {
        public string Version { get; set; } = Helper.MetadataHelper.AppVersion;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSettings TranslationSettings { get; set; } = new TranslationSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial MusicGallerySettings MusicGallerySettings { get; set; } = new MusicGallerySettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AdvancedSettings AdvancedSettings { get; set; } = new AdvancedSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsSaveConfig LyricsSaveConfig { get; set; } = new LyricsSaveConfig();
        [ObservableProperty] public partial SystemTraySettings SystemTraySettings { get; set; } = new SystemTraySettings();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MediaFolder> LocalMediaFolders { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; } = [];
        [Obsolete][ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MappedSongSearchQuery> MappedSongSearchQueries { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LyricsWindowStatus> WindowBoundsRecords { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<SongsTabInfo> StarredPlaylists { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<PluginInfo> PluginsInfo { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LyricsCardConfig> LyricsCardConfigs { get; set; } = [];

        public AppSettings() { }
    }
}
