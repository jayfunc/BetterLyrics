using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
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
            await HideAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await HideAsync();
        }

        private async Task HideAsync()
        {
            var lyricsWindowSwitchWindow = WindowHook.GetWindowByWindowType<LyricsWindowSwitchWindow>();
            lyricsWindowSwitchWindow?.ViewModel.RootGridOpacity = 0;
            await Task.Delay(300);
            WindowHook.HideWindow<LyricsWindowSwitchWindow>();
        }
    }
}
