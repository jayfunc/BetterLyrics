using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsWindowStatus : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsDefault { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MonitorDeviceName { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsWorkArea { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTop { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTopPolling { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsShownInSwitchers { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLocked { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPinToTaskbar { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsMaximized { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFullscreen { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsLayoutOrientation LyricsLayoutOrientation { get; set; } = LyricsLayoutOrientation.Horizontal;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsDisplayType LyricsDisplayType { get; set; } = LyricsDisplayType.SplitView;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect WindowBounds { get; set; } = new Rect(100, 100, 800, 500);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double DockHeight { get; set; } = 64;
        [ObservableProperty] public partial Rect DemoWindowBounds { get; set; }
        [ObservableProperty] public partial Rect MonitorBounds { get; set; }
        [ObservableProperty] public partial Rect DemoMonitorBounds { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial DockPlacement DockPlacement { get; set; } = DockPlacement.Top;
        [ObservableProperty] public partial LyricsStyleSettings LyricsStyleSettings { get; set; } = new();
        [ObservableProperty] public partial LyricsEffectSettings LyricsEffectSettings { get; set; } = new(500, 500, 500, EasingType.EaseInOutQuad);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsBackgroundSettings LyricsBackgroundSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtAreaStyleSettings AlbumArtLayoutSettings { get; set; } = new();
        [ObservableProperty] public partial AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAdaptToEnvironment { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowPixelSampleMode EnvironmentSampleMode { get; set; } = WindowPixelSampleMode.WindowEdge;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoShowOrHideWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TitleBarArea TitleBarArea { get; set; } = TitleBarArea.Top;

        [JsonIgnore][ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsOpened { get; set; } = false;

        [JsonIgnore] public DispatcherQueueTimer? VisibilityTimer { get; set; }

        public LyricsWindowStatus()
        {

        }

        public LyricsWindowStatus(Window? targetWindow = null)
        {
            UpdateMonitorNameAndBounds(targetWindow);
            UpdateDemoWindowAndMonitorBounds();
        }

        partial void OnWindowBoundsChanged(Rect value)
        {
            UpdateMonitorNameAndBounds();
            UpdateDemoWindowAndMonitorBounds();
        }

        private void UpdateMonitorNameAndBounds(Window? targetWindow = null)
        {
            targetWindow ??= WindowHook.GetWindows<NowPlayingWindow>().FirstOrDefault(x => x.LyricsWindowStatus == this);
            if (targetWindow == null) return;

            var mointor = MonitorHook.GetMonitorInfoExFromWindow(targetWindow);
            MonitorDeviceName = mointor.szDevice;
            MonitorBounds = new Rect(
                mointor.rcMonitor.Left,
                mointor.rcMonitor.Top,
                mointor.rcMonitor.Width,
                mointor.rcMonitor.Height
            );
        }

        public void UpdateMonitorBounds()
        {
            var mointor = MonitorHook.GetMonitorInfoExFromDeviceName(MonitorDeviceName);
            MonitorBounds = new Rect(
                mointor.rcMonitor.Left,
                mointor.rcMonitor.Top,
                mointor.rcMonitor.Width,
                mointor.rcMonitor.Height
            );
        }

        public void UpdateDemoWindowAndMonitorBounds(double factor = 0.1)
        {
            DemoWindowBounds = new Rect(
                (WindowBounds.X - MonitorBounds.Left) * factor,
                (WindowBounds.Y - MonitorBounds.Top) * factor,
                WindowBounds.Width * factor,
                WindowBounds.Height * factor
            );
            DemoMonitorBounds = new Rect(
                MonitorBounds.Left * factor,
                MonitorBounds.Top * factor,
                MonitorBounds.Width * factor,
                MonitorBounds.Height * factor
            );
        }

        public Rect GetWindowBoundsWhenWorkArea()
        {
            return new Rect(
                MonitorBounds.X,
                DockPlacement switch
                {
                    DockPlacement.Top => MonitorBounds.Top,
                    DockPlacement.Bottom => MonitorBounds.Bottom - DockHeight,
                    _ => MonitorBounds.Top,
                } - 1,
                MonitorBounds.Width,
                DockPlacement switch
                {
                    DockPlacement.Top => DockHeight,
                    DockPlacement.Bottom => DockHeight,
                    _ => DockHeight,
                } + 1
            );
        }

        public object Clone()
        {
            return new LyricsWindowStatus(null)
            {
                Name = this.Name,
                IsDefault = this.IsDefault,
                MonitorDeviceName = this.MonitorDeviceName,
                IsWorkArea = this.IsWorkArea,
                IsAlwaysOnTop = this.IsAlwaysOnTop,
                IsAlwaysOnTopPolling = this.IsAlwaysOnTopPolling,
                IsShownInSwitchers = this.IsShownInSwitchers,
                IsLocked = this.IsLocked,
                IsPinToTaskbar = this.IsPinToTaskbar,
                IsMaximized = this.IsMaximized,
                IsFullscreen = this.IsFullscreen,

                LyricsLayoutOrientation = this.LyricsLayoutOrientation,
                LyricsDisplayType = this.LyricsDisplayType,
                WindowBounds = this.WindowBounds,
                DockHeight = this.DockHeight,
                DemoWindowBounds = this.DemoWindowBounds,
                MonitorBounds = this.MonitorBounds,
                DemoMonitorBounds = this.DemoMonitorBounds,
                DockPlacement = this.DockPlacement,

                LyricsStyleSettings = (LyricsStyleSettings)this.LyricsStyleSettings.Clone(),
                LyricsEffectSettings = (LyricsEffectSettings)this.LyricsEffectSettings.Clone(),
                LyricsBackgroundSettings = (LyricsBackgroundSettings)this.LyricsBackgroundSettings.Clone(),
                AlbumArtLayoutSettings = (AlbumArtAreaStyleSettings)this.AlbumArtLayoutSettings.Clone(),
                AlbumArtAreaEffectSettings = (AlbumArtAreaEffectSettings)this.AlbumArtAreaEffectSettings.Clone(),

                IsAdaptToEnvironment = this.IsAdaptToEnvironment,
                EnvironmentSampleMode = this.EnvironmentSampleMode,
                AutoShowOrHideWindow = this.AutoShowOrHideWindow,
                TitleBarArea = this.TitleBarArea,
            };

        }
    }
}
