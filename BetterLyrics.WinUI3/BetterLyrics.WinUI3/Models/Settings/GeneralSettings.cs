using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class GeneralSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowMode AutoStartWindowType { get; set; } = LyricsWindowMode.StandardMode;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Language Language { get; set; } = Language.FollowSystem;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IgnoreFullscreenWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LXMusicServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool HideWindowWhenNotPlaying { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsDragEverywhereEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsImmersiveMode { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ExitOnLyricsWindowClosed { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PlayOrPauseShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "P" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> NextSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Right" };
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> PreviousSongShortcut { get; set; } = new List<string> { "Ctrl", "Alt", "Left" };

        public GeneralSettings() { }
    }
}
