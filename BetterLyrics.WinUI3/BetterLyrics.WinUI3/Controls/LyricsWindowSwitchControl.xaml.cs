using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsWindowSwitchControl : UserControl
    {
        public LyricsWindowSwitchControlViewModel ViewModel => (LyricsWindowSwitchControlViewModel)DataContext;

        public LyricsWindowSwitchControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<LyricsWindowSwitchControlViewModel>();
        }

        private async void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<NowPlayingWindow>((LyricsWindowStatus)(((FrameworkElement)sender).DataContext));
            await HideAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await HideAsync();
        }

        private async Task HideAsync()
        {
            var lyricsWindowSwitchWindow = WindowHook.GetWindow<LyricsWindowSwitchWindow>();
            lyricsWindowSwitchWindow?.ViewModel.RootGridOpacity = 0;
            await Task.Delay(300);
            lyricsWindowSwitchWindow?.HideWindow();
        }

        private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
        {
            Shadow.Receivers.Add(ShadowCastGrid);
        }

        private async void SettingsHypelinkButton_Click(object sender, RoutedEventArgs e)
        {
            await HideAsync();
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }
    }
}
