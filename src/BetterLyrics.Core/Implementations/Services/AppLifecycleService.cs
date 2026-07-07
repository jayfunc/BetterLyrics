using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.Implementations.Services;

public partial class AppLifecycleService : ObservableObject, IAppLifecycleService
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    [RelayCommand]
    public void RestartApp()
    {
        _windowManagerProvider.RestartApp();
    }

    [RelayCommand]
    public void ExitApp()
    {
        _windowManagerProvider.ExitApp();
    }
}