using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System;
using WinUIEx;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel(ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty]
        public partial string ToolTipText { get; set; } = Constants.App.AppName;

        [RelayCommand]
        private static void ExitApp()
        {
            WindowHelper.ExitApp();
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHelper.RestartApp();
        }

        [RelayCommand]
        private static void ResetWindowPosition()
        {
            var lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            lyricsWindow?.MoveAndResize(100, 100, 800, 500);
        }

        [RelayCommand]
        private static void OpenSettings()
        {
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private static void OpenMusicGallery()
        {
            WindowHelper.OpenOrShowWindow<MusicGalleryWindow>();
        }

        [RelayCommand]
        private static void OpenLyrics()
        {
            WindowHelper.OpenOrShowWindow<LyricsWindow>();
        }

        [RelayCommand]
        private static void OpenLyricsWindowSwitch()
        {
            WindowHelper.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }
    }
}
