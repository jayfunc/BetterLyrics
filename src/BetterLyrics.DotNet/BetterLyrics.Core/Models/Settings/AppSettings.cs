using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AppSettings : ObservableRecipient
{
    public string Version { get; set; } = MetadataHelper.AppVersion;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial TranslationSettings TranslationSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial GeneralSettings GeneralSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MusicGallerySettings MusicGallerySettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsCardSettings LyricsCardSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AdvancedSettings AdvancedSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsSaveConfig LyricsSaveConfig { get; set; } = new();

    [ObservableProperty] public partial SystemTraySettings SystemTraySettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial DiscordSettings DiscordSettings { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<MediaFolder> LocalMediaFolders { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; } = [];

    [Obsolete]
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<MappedSongSearchQuery> MappedSongSearchQueries { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<LyricsWindowStatus> WindowBoundsRecords { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<SongsTabInfo> StarredPlaylists { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<PluginInfo> PluginsInfo { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<LyricsCardConfig> LyricsCardConfigs { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<LayoutProfile> LayoutProfiles { get; set; } = [];
}