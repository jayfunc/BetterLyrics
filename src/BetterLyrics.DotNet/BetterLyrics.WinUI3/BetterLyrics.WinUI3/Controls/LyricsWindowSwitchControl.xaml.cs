using System.Threading.Tasks;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsWindowSwitchControl : UserControl
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public LyricsWindowSwitchControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LyricsWindowSwitchControlViewModel>();
    }

    public LyricsWindowSwitchControlViewModel ViewModel => (LyricsWindowSwitchControlViewModel)DataContext;

    private async void Grid_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await HideAsync();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await HideAsync();
    }

    private async Task HideAsync()
    {
        var lyricsWindowSwitchWindow = _windowManagerProvider.GetWindow<LyricsWindowSwitchWindow>();
        lyricsWindowSwitchWindow?.ViewModel.RootGridOpacity = 0;
        await Task.Delay(300);
        if (lyricsWindowSwitchWindow != null) _windowManagerProvider.HideWindow(lyricsWindowSwitchWindow);
    }

    private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
    {
        Shadow.Receivers.Add(ShadowCastGrid);
    }

    private async void SettingsHypelinkButton_Click(object sender, RoutedEventArgs e)
    {
        await HideAsync();
        _windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
        var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        settingsPageViewModel.NavigateToSection(SettingsSection.LyricsWindowMgr);
    }

    private async void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        int index = -1;
        var key = e.Key;
        if (key >= Windows.System.VirtualKey.Number1 && key <= Windows.System.VirtualKey.Number9)
        {
            index = key - Windows.System.VirtualKey.Number1;
        }
        else if (key == Windows.System.VirtualKey.Number0)
        {
            index = 9;
        }
        else if (key >= Windows.System.VirtualKey.NumberPad1 && key <= Windows.System.VirtualKey.NumberPad9)
        {
            index = key - Windows.System.VirtualKey.NumberPad1;
        }
        else if (key == Windows.System.VirtualKey.NumberPad0)
        {
            index = 9;
        }
        else if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
        {
            index = 10 + (key - Windows.System.VirtualKey.A);
        }

        if (index >= 0 && index < ViewModel.AppSettings.WindowBoundsRecords.Count)
        {
            e.Handled = true;
            var status = ViewModel.AppSettings.WindowBoundsRecords[index];
            await HideAsync();

            var settingsService = Ioc.Default.GetRequiredService<BetterLyrics.Core.Interfaces.Services.ISettingsService>();
            
            if (settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
            {
                _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
            }
            else
            {
                var openedWindows = _windowManagerProvider.GetWindows<NowPlayingWindow>();
                foreach (var item in System.Linq.Enumerable.Where(openedWindows, x => x.LyricsWindowStatus != status))
                    _windowManagerProvider.CloseWindow(item);

                _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
            }
        }
    }
}