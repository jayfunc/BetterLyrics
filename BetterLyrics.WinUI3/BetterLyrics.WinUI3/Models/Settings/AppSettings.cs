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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings StandardLyricsStyleSettings { get; set; } = new LyricsStyleSettings(32, TextAlignmentType.Left, 0);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings DesktopLyricsStyleSettings { get; set; } = new LyricsStyleSettings(28, TextAlignmentType.Center, 2);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings DockLyricsStyleSettings { get; set; } = new LyricsStyleSettings(16, TextAlignmentType.Center, 0);

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings StandardLyricsEffectSettings { get; set; } = new LyricsEffectSettings(100, 500, 1000, EasingType.EaseInOutSine);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings DesktopLyricsEffectSettings { get; set; } = new LyricsEffectSettings(500, 500, 500, EasingType.EaseInOutSine);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings DockLyricsEffectSettings { get; set; } = new LyricsEffectSettings(500, 500, 500, EasingType.EaseInOutSine);

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial StandardModeSettings StandardModeSettings { get; set; } = new StandardModeSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DesktopModeSettings DesktopModeSettings { get; set; } = new DesktopModeSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DockModeSettings DockModeSettings { get; set; } = new DockModeSettings();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsBackgroundSettings LyricsBackgroundSettings { get; set; } = new LyricsBackgroundSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtLayoutSettings AlbumArtLayoutSettings { get; set; } = new AlbumArtLayoutSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TranslationSettings TranslationSettings { get; set; } = new TranslationSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial MusicGallerySettings MusicGallerySettings { get; set; } = new MusicGallerySettings();

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LocalMediaFolder> LocalMediaFolders { get; set; } = [];
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<MediaSourceProviderInfo> MediaSourceProvidersInfo { get; set; } = [];

        public AppSettings() { }
    }
}
