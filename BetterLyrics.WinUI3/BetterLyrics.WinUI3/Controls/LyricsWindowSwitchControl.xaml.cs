using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
            var lyricsWindowSwitchWindow = WindowHelper.GetWindowByWindowType<LyricsWindowSwitchWindow>();
            lyricsWindowSwitchWindow?.ViewModel.RootGridOpacity = 0;
            await Task.Delay(300);
            WindowHelper.HideWindow<LyricsWindowSwitchWindow>();
        }
    }
}
