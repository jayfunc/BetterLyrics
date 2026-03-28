using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.Services.AppLifecycleService
{
    public interface IAppLifecycleService
    {
        IRelayCommand RestartAppCommand { get; }
        IRelayCommand ExitAppCommand { get; }
    }
}
