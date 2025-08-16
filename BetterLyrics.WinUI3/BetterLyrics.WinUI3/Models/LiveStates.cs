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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowMode CurrentLyricsWindowMode { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsDisplayType CurrentLyricsDisplayType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings CurrentLyricsStyleSettings { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings CurrentLyricsEffectSettings { get; set; }

        public LiveStates(AppSettings appSettings)
        {
            CurrentLyricsWindowMode = LyricsWindowMode.StandardMode;
            CurrentLyricsDisplayType = appSettings.StandardModeSettings.LyricsDisplayType;
            CurrentLyricsStyleSettings = appSettings.StandardLyricsStyleSettings;
            CurrentLyricsEffectSettings = appSettings.StandardLyricsEffectSettings;
        }

        public void ToggleLyricsWindowMode(LyricsWindowMode mode)
        {
            if (CurrentLyricsWindowMode == mode)
            {
                CurrentLyricsWindowMode = LyricsWindowMode.StandardMode;
            }
            else
            {
                CurrentLyricsWindowMode = mode;
            }
        }
    }
}
