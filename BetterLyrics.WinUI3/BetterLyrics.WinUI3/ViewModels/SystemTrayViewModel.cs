using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayViewModel : BaseViewModel, IRecipient<PropertyChangedMessage<bool>>
    {
        public SystemTrayViewModel(ISettingsService settingsService) : base(settingsService) { }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsWindowLocked { get; set; } = false;

        [ObservableProperty]
        public partial string ToolTipText { get; set; } = AppInfo.AppName;

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
        private void ExitApp()
        {
            WindowHelper.ExitAllWindows();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            // 打开设置窗口
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private void UnlockWindow()
        {
            var window = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            DesktopModeHelper.Unlock(window);
            IsLyricsWindowLocked = false;
        }
    }
}
