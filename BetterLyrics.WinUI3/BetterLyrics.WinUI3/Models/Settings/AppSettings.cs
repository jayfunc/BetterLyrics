using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AppSettings : ObservableRecipient
    {
        public string Version { get; set; } = Helper.MetadataHelper.AppVersion;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSettings TranslationSettings { get; set; } = new TranslationSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial MusicGallerySettings MusicGallerySettings { get; set; } = new MusicGallerySettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AdvancedSettings AdvancedSettings { get; set; } = new AdvancedSettings();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LocalMediaFolder> LocalMediaFolders { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MappedSongSearchQuery> MappedSongSearchQueries { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LyricsWindowStatus> WindowBoundsRecords { get; set; } = [];

        public AppSettings() { }
    }
}
