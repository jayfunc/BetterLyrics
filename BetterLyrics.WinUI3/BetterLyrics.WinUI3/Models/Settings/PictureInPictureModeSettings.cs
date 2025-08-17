using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class PictureInPictureModeSettings : BaseModeSettings
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial List<string> ToggleShortcut { get; set; } = new List<string>() { "Ctrl", "Shift", "P" };

        public PictureInPictureModeSettings()
        {
            LyricsDisplayType = Enums.LyricsDisplayType.SplitView;
        }
    }
}
