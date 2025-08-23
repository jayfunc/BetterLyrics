// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        public string Version { get; set; } = MetadataHelper.AppVersion;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "App";

        [ObservableProperty]
        public partial ObservableCollection<LibInfo> Libs { get; set; }

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            AppSettings = _settingsService.AppSettings;

            Libs =
            [
                new LibInfo { Name = "3v.EvtSource" },
                new LibInfo { Name = "ColorThief.ImageSharp" },
                new LibInfo { Name = "CommunityToolkit.Labs.WinUI.MarqueeText" },
                new LibInfo { Name = "CommunityToolkit.Labs.WinUI.OpacityMaskView" },
                new LibInfo { Name = "CommunityToolkit.Labs.WinUI.Shimmer" },
                new LibInfo { Name = "CommunityToolkit.Mvvm" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Behaviors" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Controls.MetadataControl" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Controls.Primitives" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Controls.Segmented" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Controls.SettingsControls" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Controls.Sizers" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Converters" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Extensions" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Helpers" },
                new LibInfo { Name = "CommunityToolkit.WinUI.Media" },
                new LibInfo { Name = "csharp-pinyin" },
                new LibInfo { Name = "Dubya.WindowsMediaController" },
                new LibInfo { Name = "H.NotifyIcon.WinUI"},
                new LibInfo { Name = "Hqub.Last.fm"},
                new LibInfo { Name = "Lyricify.Lyrics.Helper-NativeAot"},
                new LibInfo { Name = "Microsoft.Extensions.DependencyInjection"},
                new LibInfo { Name = "Microsoft.Extensions.Logging"},
                new LibInfo { Name = "Microsoft.Graphics.Win2D"},
                new LibInfo { Name = "Microsoft.Windows.SDK.BuildTools"},
                new LibInfo { Name = "Microsoft.WindowsAppSDK"},
                new LibInfo { Name = "Nito.AsyncEx"},
                new LibInfo { Name = "Nito.AsyncEx.Tasks"},
                new LibInfo { Name = "NTextCat"},
                new LibInfo { Name = "RomajiConverter.Core"},
                new LibInfo { Name = "Serilog.Extensions.Logging"},
                new LibInfo { Name = "Serilog.Sinks.File"},
                new LibInfo { Name = "ShadowViewer.Controls.Notification"},
                new LibInfo { Name = "SixLabors.ImageSharp" },
                new LibInfo { Name = "System.Drawing.Common"},
                new LibInfo { Name = "System.Text.Encoding.CodePages"},
                new LibInfo { Name = "TagLibSharp" },
                new LibInfo { Name = "TinyPinyin.Net"},
                new LibInfo { Name = "Ude.NetStandard"},
                new LibInfo { Name = "Vanara.PInvoke.CoreAudio"},
                new LibInfo { Name = "Vanara.PInvoke.DwmApi"},
                new LibInfo { Name = "Vanara.PInvoke.Gdi32"},
                new LibInfo { Name = "Vanara.PInvoke.Shell32"},
                new LibInfo { Name = "Vanara.PInvoke.User32"},
                new LibInfo { Name = "WinUIEx"   },
                new LibInfo { Name = "z440.atl.core" },
            ];
        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(Constants.Link.GitHubUrl));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(Helper.PathHelper.CacheFolder);
        }

        [RelayCommand]
        private static void RestartApp()
        {
            Helper.WindowHelper.RestartApp();
        }

        [RelayCommand]
        private async Task ImportSettingsAsync()
        {
            var window = WindowHelper.GetWindowByWindowType<SettingsWindow>();
            if (window == null) return;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".json");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                var succeed = _settingsService.ImportSettings(file.Path);
                if (succeed)
                {
                    WindowHelper.RestartApp();
                }
                else
                {
                    App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("ImportSettingsFailed") ?? "");
                }
            }
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            var window = WindowHelper.GetWindowByWindowType<SettingsWindow>();
            if (window == null) return;

            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                _settingsService.ExportSettings(folder.Path);
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("ExportSettingsSuccess") ?? "", InfoBarSeverity.Success);
            }
        }
    }
}
