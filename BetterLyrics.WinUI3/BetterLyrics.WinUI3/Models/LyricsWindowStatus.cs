using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using System;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsWindowStatus : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Name { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsDefault { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string MonitorDeviceName { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsWorkArea { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsBorderless { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTop { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAlwaysOnTopPolling { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsShownInSwitchers { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsClickThrough { get; set; } = false;
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
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AlbumArtLayoutSettings AlbumArtLayoutSettings { get; set; } = new();
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsAdaptToEnvironment { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial WindowPixelSampleMode EnvironmentSampleMode { get; set; } = WindowPixelSampleMode.WindowEdge;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool AutoShowOrHideWindow { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial TitleBarArea TitleBarArea { get; set; } = TitleBarArea.Top;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double WindowX { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double WindowY { get; set; } = 100;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double WindowWidth { get; set; } = 800;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double WindowHeight { get; set; } = 500;

        public LyricsWindowStatus()
        {
            UpdateMonitorNameAndBounds();
            UpdateDemoWindowAndMonitorBounds();
        }

        partial void OnWindowXChanged(double value)
        {
            WindowBounds = WindowBounds.WithX(value);
        }

        partial void OnWindowYChanged(double value)
        {
            WindowBounds = WindowBounds.WithY(value);
        }

        partial void OnWindowWidthChanged(double value)
        {
            WindowBounds = WindowBounds.WithWidth(value);
        }

        partial void OnWindowHeightChanged(double value)
        {
            WindowBounds = WindowBounds.WithHeight(value);
        }

        partial void OnLyricsStyleSettingsChanged(LyricsStyleSettings oldValue, LyricsStyleSettings newValue)
        {
            oldValue.PropertyChanged -= OldLyricsStyleSettings_PropertyChanged;
            newValue.PropertyChanged += OldLyricsStyleSettings_PropertyChanged;
        }

        partial void OnLyricsEffectSettingsChanged(LyricsEffectSettings oldValue, LyricsEffectSettings newValue)
        {
            oldValue.PropertyChanged -= OldLyricsEffectSettings_PropertyChanged;
            newValue.PropertyChanged += OldLyricsEffectSettings_PropertyChanged;
        }

        partial void OnLyricsBackgroundSettingsChanged(LyricsBackgroundSettings oldValue, LyricsBackgroundSettings newValue)
        {
            oldValue.PropertyChanged -= OldLyricsBackgroundSettings_PropertyChanged;
            newValue.PropertyChanged += OldLyricsBackgroundSettings_PropertyChanged;
        }

        partial void OnAlbumArtLayoutSettingsChanged(AlbumArtLayoutSettings oldValue, AlbumArtLayoutSettings newValue)
        {
            oldValue.PropertyChanged -= OldAlbumArtLayoutSettings_PropertyChanged;
            newValue.PropertyChanged += OldAlbumArtLayoutSettings_PropertyChanged;
        }

        private void OldLyricsStyleSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(LyricsStyleSettings));
        }

        private void OldLyricsEffectSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(LyricsEffectSettings));
        }

        private void OldLyricsBackgroundSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(LyricsBackgroundSettings));
        }

        private void OldAlbumArtLayoutSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(AlbumArtLayoutSettings));
        }

        partial void OnWindowBoundsChanged(Rect value)
        {
            UpdateMonitorNameAndBounds();
            UpdateDemoWindowAndMonitorBounds();
        }

        partial void OnAutoShowOrHideWindowChanged(bool value)
        {
            WindowHelper.SetLyricsWindowVisibilityByPlayingStatus();
        }

        public void UpdateMonitorNameAndBounds()
        {
            var lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow == null) return;

            var mointor = MonitorHelper.GetMonitorInfoExFromWindow(lyricsWindow);
            MonitorDeviceName = mointor.szDevice;
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

        public object Clone()
        {
            return new LyricsWindowStatus
            {
                Name = this.Name,
                IsDefault = this.IsDefault,
                MonitorDeviceName = this.MonitorDeviceName,
                IsWorkArea = this.IsWorkArea,
                IsBorderless = this.IsBorderless,
                IsAlwaysOnTop = this.IsAlwaysOnTop,
                IsAlwaysOnTopPolling = this.IsAlwaysOnTopPolling,
                IsShownInSwitchers = this.IsShownInSwitchers,
                IsClickThrough = this.IsClickThrough,
                LyricsLayoutOrientation = this.LyricsLayoutOrientation,
                LyricsDisplayType = this.LyricsDisplayType,
                WindowBounds = this.WindowBounds,
                DockHeight = this.DockHeight,
                DemoWindowBounds = this.DemoWindowBounds,
                MonitorBounds = this.MonitorBounds,
                DemoMonitorBounds = this.DemoMonitorBounds,
                LyricsStyleSettings = (LyricsStyleSettings)this.LyricsStyleSettings.Clone(),
                LyricsEffectSettings = (LyricsEffectSettings)this.LyricsEffectSettings.Clone(),
                LyricsBackgroundSettings = (LyricsBackgroundSettings)this.LyricsBackgroundSettings.Clone(),
                AlbumArtLayoutSettings = (AlbumArtLayoutSettings)this.AlbumArtLayoutSettings.Clone(),
                IsAdaptToEnvironment = this.IsAdaptToEnvironment,
                EnvironmentSampleMode = this.EnvironmentSampleMode,
                AutoShowOrHideWindow = this.AutoShowOrHideWindow,
                TitleBarArea = this.TitleBarArea,
            };
        }
    }

    public static class LyricsWindowStatusExtensions
    {
        public static LyricsWindowStatus DesktopMode()
        {
            return new LyricsWindowStatus
            {
                Name = App.ResourceLoader!.GetString("DesktopMode"),
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                WindowBounds = new Rect(100, 100, 600, 250),
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsBorderless = true,
                IsClickThrough = true,
                IsAdaptToEnvironment = true,
                EnvironmentSampleMode = WindowPixelSampleMode.WindowEdge,
                LyricsStyleSettings = new()
                {
                    LyricsFontSize = 20,
                    LyricsAlignmentType = TextAlignmentType.Center,
                },
                LyricsBackgroundSettings = new LyricsBackgroundSettings
                {
                    IsFluidOverlayEnabled = false,
                }
            };
        }

        public static LyricsWindowStatus DockedMode()
        {
            return new LyricsWindowStatus
            {
                Name = App.ResourceLoader!.GetString("DockedMode"),
                IsWorkArea = true,
                IsAlwaysOnTop = true,
                IsAlwaysOnTopPolling = true,
                IsBorderless = true,
                IsAdaptToEnvironment = true,
                LyricsDisplayType = LyricsDisplayType.LyricsOnly,
                EnvironmentSampleMode = WindowPixelSampleMode.BelowWindow,
                TitleBarArea = TitleBarArea.None,
                LyricsStyleSettings = new LyricsStyleSettings
                {
                    LyricsAlignmentType = TextAlignmentType.Center,
                    LyricsFontSize = 18,
                },
                LyricsBackgroundSettings = new LyricsBackgroundSettings
                {
                    IsFluidOverlayEnabled = false,
                    IsPureColorOverlayEnabled = true,
                }
            };
        }

        public static LyricsWindowStatus FullscreenMode(Rect monitorBounds)
        {
            return new LyricsWindowStatus
            {
                Name = App.ResourceLoader!.GetString("FullscreenMode"),
                WindowBounds = monitorBounds,
                IsAlwaysOnTop = true,
                IsBorderless = true,
                TitleBarArea = Enums.TitleBarArea.None,
                LyricsLayoutOrientation = Enums.LyricsLayoutOrientation.Vertical,
                LyricsStyleSettings = new LyricsStyleSettings
                {
                    LyricsFontSize = 72,
                    LyricsAlignmentType = Enums.TextAlignmentType.Center,
                },
                AlbumArtLayoutSettings = new AlbumArtLayoutSettings
                {
                    AutoAlbumArtSize = false,
                    AlbumArtSize = 128,
                    SongInfoFontSize = 36,
                }
            };
        }

        public static LyricsWindowStatus StandardMode()
        {
            return new LyricsWindowStatus
            {
                Name = App.ResourceLoader!.GetString("StandardMode"),
            };
        }

        public static LyricsWindowStatus NarrowMode()
        {
            return new LyricsWindowStatus
            {
                Name = App.ResourceLoader!.GetString("NarrowMode"),
                WindowBounds = new Rect(100, 100, 400, 800),
                LyricsLayoutOrientation = LyricsLayoutOrientation.Vertical,
            };
        }
    }
}
