using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class SystemTray : UserControl
{
    private readonly UISettings _uiSettings;

    private readonly IAppUIThreadProvider _appUiThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    public SystemTrayViewModel ViewModel => (SystemTrayViewModel)DataContext;


    public SystemTray()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SystemTrayViewModel>();

        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        _appUiThreadProvider.Execute(() =>
        {
            ViewModel.UpdateSystemTrayIcon();
        });
    }

    private void TrayIcon_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateSystemTrayIcon();
    }
}