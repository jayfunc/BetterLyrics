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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AutoStartWindowType AutoStartWindowType { get; set; } = AutoStartWindowType.StandardMode;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Language Language { get; set; } = Language.FollowSystem;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IgnoreFullscreenWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LXMusicServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool HideWindowWhenNotPlaying { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsDragEverywhereEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsDisplayType DisplayType { get; set; } = LyricsDisplayType.SplitView;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsImmersiveMode { get; set; } = false;

        public GeneralSettings() { }
    }
}
