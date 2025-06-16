using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsPageViewModel(ISettingsService settingsService)
        : BaseViewModel(settingsService)
    {
        [ObservableProperty]
        public partial bool IsSettingsPopupOpened { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double LimitedLineWidth { get; set; } = 0.0;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double LimitedLineHeight { get; set; } = 0.0;

        [RelayCommand]
        private void ToggleSettingsPopup()
        {
            IsSettingsPopupOpened = !IsSettingsPopupOpened;
        }
    }
}
