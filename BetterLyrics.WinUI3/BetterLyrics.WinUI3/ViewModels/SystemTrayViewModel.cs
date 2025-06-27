using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel
        : BaseViewModel,
            IRecipient<PropertyChangedMessage<bool>>
    {
        [ObservableProperty]
        public partial string ToolTipText { get; set; } = AppInfo.AppName;

        [ObservableProperty]
        public partial bool IsLyricsWindowLocked { get; set; } = false;

        public SystemTrayViewModel(ISettingsService settingsService)
            : base(settingsService) { }

        [RelayCommand]
        private void OpenSettings()
        {
            // 打开设置窗口
            WindowHelper.OpenSettingsWindow();
        }

        [RelayCommand]
        private void ExitApp()
        {
            // 退出应用程序
            App.Current.Exit();
        }

        [RelayCommand]
        private void UnlockWindow()
        {
            var window = WindowHelper.GetWindowByFramePageType(typeof(LyricsPage));
            DesktopModeHelper.Unlock(window);
            IsLyricsWindowLocked = false;
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is HostWindowViewModel)
            {
                if (message.PropertyName == nameof(HostWindowViewModel.IsLyricsWindowLocked))
                {
                    IsLyricsWindowLocked = message.NewValue;
                }
            }
        }
    }
}
