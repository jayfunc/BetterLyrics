using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsSearchControl : UserControl
    {
        public LyricsSearchControlViewModel ViewModel => (LyricsSearchControlViewModel)DataContext;

        public LyricsSearchControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<LyricsSearchControlViewModel>();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedLyricsLine = e.OriginalSource as LyricsLine;
        }

    }
}
