using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class GeneralSettings : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LanguageCode { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LXMusicServer { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string AmllTtmlDbBaseUrl { get; set; } = AmllTTmlDB.BaseUrl;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial List<string> ShowOrHideLyricsWindowShortcut { get; set; } = new() { "Ctrl", "Alt", "H" };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ExitOnLyricsWindowClosed { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ListenOnNewPlaybackSource { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IgnoreCacheWhenSearching { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int MaxAutoRetryCount { get; set; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial List<string> LyricsWindowSwitchShortcut { get; set; } = new() { "Ctrl", "Alt", "S" };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial List<string> PlayOrPauseShortcut { get; set; } = new() { "Ctrl", "Alt", "P" };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial List<string> NextSongShortcut { get; set; } = new() { "Ctrl", "Alt", "Right" };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial List<string> PreviousSongShortcut { get; set; } = new() { "Ctrl", "Alt", "Left" };

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool MultiNowPlayingWindowMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool AutoStartLyricsWindow { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool EnhanceControlInteractiveAnimations { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppTheme AppTheme { get; set; } = AppTheme.Default;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowSplashScreen { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial DateTime LastAppUpateCheckDateTime { get; set; } = DateTime.Now;
}