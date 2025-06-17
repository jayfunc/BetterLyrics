using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SystemTrayPageViewModel : BaseViewModel
    {
        public SystemTrayPageViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            switch (_settingsService.AutoStartWindowType)
            {
                case AutoStartWindowType.None:
                    break;
                case AutoStartWindowType.InAppLyrics:
                    WindowHelper.OpenInAppLyricsWindow();
                    break;
                case AutoStartWindowType.DesktopLyrics:
                    WindowHelper.OpenDesktopLyricsWindow();
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            WindowHelper.OpenSettingsWindow();
        }

        [RelayCommand]
        private void ExitApp()
        {
            Application.Current.Exit();
        }
    }
}
