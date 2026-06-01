using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class GeneralSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LanguageCode { get; set; } = "";
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LXMusicServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string AmllTtmlDbBaseUrl { get; set; } = Constants.AmllTTmlDB.BaseUrl;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> ShowOrHideLyricsWindowShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "H" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ExitOnLyricsWindowClosed { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ListenOnNewPlaybackSource { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IgnoreCacheWhenSearching { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> LyricsWindowSwitchShortcut { get; set; } = new List<string>() { "Ctrl", "Alt", "S" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PlayOrPauseShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "P" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> NextSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Right" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PreviousSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Left" };

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool MultiNowPlayingWindowMode { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoStartLyricsWindow { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool EnhanceControlInteractiveAnimations { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ElementTheme AppTheme { get; set; } = ElementTheme.Default;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowSplashScreen { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DateTime LastAppUpateCheckDateTime { get; set; } = DateTime.Now;

        public GeneralSettings() { }
    }
}
