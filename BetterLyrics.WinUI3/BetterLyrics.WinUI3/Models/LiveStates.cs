using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LiveStates : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowMode LyricsWindowMode { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsDisplayType LyricsDisplayType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTop { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings LyricsStyleSettings { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings LyricsEffectSettings { get; set; }

        public LiveStates(AppSettings appSettings)
        {
            LyricsWindowMode = LyricsWindowMode.StandardMode;
            LyricsDisplayType = appSettings.StandardModeSettings.LyricsDisplayType;
            LyricsStyleSettings = appSettings.StandardLyricsStyleSettings;
            LyricsEffectSettings = appSettings.StandardLyricsEffectSettings;
            IsAlwaysOnTop = false;
        }

        public void ToggleLyricsWindowMode(LyricsWindowMode mode)
        {
            if (LyricsWindowMode == mode)
            {
                LyricsWindowMode = LyricsWindowMode.StandardMode;
            }
            else
            {
                LyricsWindowMode = mode;
            }
        }
    }
}
