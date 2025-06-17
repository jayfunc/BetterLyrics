using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsPageViewModel(ISettingsService settingsService)
        : BaseViewModel(settingsService)
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial double LimitedLineWidth { get; set; } = 0.0;

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            WindowHelper.OpenSettingsWindow();
        }
    }
}
