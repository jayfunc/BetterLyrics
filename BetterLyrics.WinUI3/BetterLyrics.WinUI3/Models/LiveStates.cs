using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LiveStates : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowMode LyricsWindowMode { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsDisplayType LyricsDisplayType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTop { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsStyleSettings LyricsStyleSettings { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsEffectSettings LyricsEffectSettings { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect LyricsWindowBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect LyricsWindowMonitorBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect DemoLyricsWindowBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect DemoLyricsWindowMonitorBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LyricsWindowMonitorName { get; set; }

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

        partial void OnLyricsWindowBoundsChanged(Rect value)
        {
            double factor = 0.1;
            DemoLyricsWindowBounds = new Rect(
                value.X * factor,
                value.Y * factor,
                value.Width * factor,
                value.Height * factor
            );
            var lyricsWindow = WindowHelper.GetWindowByWindowType<Views.LyricsWindow>();
            if (lyricsWindow == null) return;
            var mointor = MonitorHelper.GetMonitorInfoExFromWindow(lyricsWindow);
            LyricsWindowMonitorName = mointor.szDevice;
            LyricsWindowMonitorBounds = new Rect(
                mointor.rcWork.Left,
                mointor.rcWork.Top,
                mointor.rcWork.Width,
                mointor.rcWork.Height
            );
            DemoLyricsWindowMonitorBounds = new Rect(
                mointor.rcWork.Left * factor,
                mointor.rcWork.Top * factor,
                mointor.rcWork.Width * factor,
                mointor.rcWork.Height * factor
            );
        }
    }
}
