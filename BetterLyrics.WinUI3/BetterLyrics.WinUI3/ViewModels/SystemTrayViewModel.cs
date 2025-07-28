using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel(ISettingsService settingsService) : BaseViewModel(settingsService), IRecipient<PropertyChangedMessage<bool>>
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsWindowLocked { get; set; } = false;

        [ObservableProperty]
        public partial string ToolTipText { get; set; } = MetadataHelper.AppName;

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsLyricsWindowLocked))
                {
                    if (IsLyricsWindowLocked != message.NewValue)
                    {
                        IsLyricsWindowLocked = message.NewValue;
                    }
                }
            }
        }

        [RelayCommand]
        private static void ExitApp()
        {
            LyricsWindow? lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow != null)
            {
                DockModeHelper.Disable(lyricsWindow);
            }
            Environment.Exit(0);
        }

        [RelayCommand]
        private static void OpenSettings()
        {
            WindowHelper.OpenWindow<SettingsWindow>();
        }

        [RelayCommand]
        private void UnlockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (window == null) return;

            DesktopModeHelper.SetClickThrough(window, false);
            IsLyricsWindowLocked = false;
        }
    }
}
