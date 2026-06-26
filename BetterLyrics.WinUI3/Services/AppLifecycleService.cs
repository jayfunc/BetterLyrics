using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Hooks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.Services
{
    public partial class AppLifecycleService : ObservableObject, IAppLifecycleService
    {
        [RelayCommand]
        public void RestartApp() => WindowHook.RestartApp();

        [RelayCommand]
        public void ExitApp() => WindowHook.ExitApp();
    }
}
