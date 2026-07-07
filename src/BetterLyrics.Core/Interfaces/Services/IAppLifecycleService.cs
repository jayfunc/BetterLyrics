using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IAppLifecycleService
{
    IRelayCommand RestartAppCommand { get; }
    IRelayCommand ExitAppCommand { get; }
}