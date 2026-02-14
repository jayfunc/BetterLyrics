using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models.Settings
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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysHideUnlockButton { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool KeepNowPlayingBarInteractiveWhenLocked { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTimelineLyricsPreviewEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsPinToTaskbar { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TaskbarPlacement TaskbarPlacement { get; set; } = TaskbarPlacement.Right;

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
        [ObservableProperty] public partial LyricsEffectSettings LyricsEffectSettings { get; set; } = new(500, 500, 500, EasingType.Quad);
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsBackgroundSettings LyricsBackgroundSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtAreaStyleSettings AlbumArtLayoutSettings { get; set; } = new();
        [ObservableProperty] public partial AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAdaptToEnvironment { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowPixelSampleMode EnvironmentSampleMode { get; set; } = WindowPixelSampleMode.WindowEdge;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoShowOrHideWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TitleBarArea TitleBarArea { get; set; } = TitleBarArea.Top;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsKeepScreenOpen { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FPS FPS { get; set; } = FPS.Hz60;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowDebugOverlay { get; set; } = false;

        [JsonIgnore][ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowStatus WindowStatus { get; set; } = WindowStatus.Closed;
        [JsonIgnore] public DispatcherQueueTimer? VisibilityTimer { get; set; }

        public LyricsWindowStatus()
        {
            LyricsStyleSettings.PropertyChanged += LyricsStyleSettings_PropertyChanged;
            LyricsEffectSettings.PropertyChanged += LyricsEffectSettings_PropertyChanged;
            LyricsBackgroundSettings.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
            AlbumArtLayoutSettings.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
            AlbumArtAreaEffectSettings.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;
        }

        public LyricsWindowStatus(Window? targetWindow = null) : this()
        {
            UpdateMonitorNameAndBounds(targetWindow);
            UpdateDemoWindowAndMonitorBounds();
        }

        partial void OnLyricsStyleSettingsChanged(LyricsStyleSettings oldValue, LyricsStyleSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsStyleSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsStyleSettings_PropertyChanged;
        }

        private void LyricsStyleSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsStyleSettings));
        }

        partial void OnLyricsEffectSettingsChanged(LyricsEffectSettings oldValue, LyricsEffectSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsEffectSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsEffectSettings_PropertyChanged;
        }

        private void LyricsEffectSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsEffectSettings));
        }

        partial void OnLyricsBackgroundSettingsChanged(LyricsBackgroundSettings oldValue, LyricsBackgroundSettings newValue)
        {
            oldValue.PropertyChanged -= LyricsBackgroundSettings_PropertyChanged;
            newValue.PropertyChanged += LyricsBackgroundSettings_PropertyChanged;
        }

        private void LyricsBackgroundSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsBackgroundSettings));
        }

        partial void OnAlbumArtLayoutSettingsChanged(AlbumArtAreaStyleSettings oldValue, AlbumArtAreaStyleSettings newValue)
        {
            oldValue.PropertyChanged -= AlbumArtLayoutSettings_PropertyChanged;
            newValue.PropertyChanged += AlbumArtLayoutSettings_PropertyChanged;
        }

        private void AlbumArtLayoutSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtLayoutSettings));
        }

        partial void OnAlbumArtAreaEffectSettingsChanged(AlbumArtAreaEffectSettings oldValue, AlbumArtAreaEffectSettings newValue)
        {
            oldValue.PropertyChanged -= AlbumArtAreaEffectSettings_PropertyChanged;
            newValue.PropertyChanged += AlbumArtAreaEffectSettings_PropertyChanged;
        }

        private void AlbumArtAreaEffectSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtAreaEffectSettings));
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
                IsAlwaysHideUnlockButton = this.IsAlwaysHideUnlockButton,

                IsPinToTaskbar = this.IsPinToTaskbar,
                TaskbarPlacement = this.TaskbarPlacement,

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
                IsKeepScreenOpen = this.IsKeepScreenOpen,

                FPS = this.FPS,
                ShowDebugOverlay = this.ShowDebugOverlay,

                IsTimelineLyricsPreviewEnabled = this.IsTimelineLyricsPreviewEnabled,
                KeepNowPlayingBarInteractiveWhenLocked = this.KeepNowPlayingBarInteractiveWhenLocked,
            };

        }
    }
}
