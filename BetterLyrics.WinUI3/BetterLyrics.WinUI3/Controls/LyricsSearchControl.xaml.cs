using BetterLyrics.WinUI3.Models.Lyrics;
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

        private void PlayLyricsLineButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var lyricsLine = (LyricsLine)((Button)sender).DataContext;
            ViewModel.PlayLyricsLine(lyricsLine);
        }
    }
}
