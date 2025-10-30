using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class GeneralSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LanguageCode { get; set; } =  LanguageHelper.GetDefaultDisplayLanguageCode();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LXMusicServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> ShowOrHideLyricsWindowShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "H" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ExitOnLyricsWindowClosed { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ListenOnNewPlaybackSource { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> BorderlessShortcut { get; set; } = new List<string>() { "Ctrl", "Alt", "B" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> ClickThroughShortcut { get; set; } = new List<string>() { "Ctrl", "Alt", "C" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> LyricsWindowSwitchShortcut { get; set; } = new List<string>() { "Ctrl", "Alt", "S" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PlayOrPauseShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "P" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> NextSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Right" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PreviousSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Left" };

        public GeneralSettings() { }
    }
}
