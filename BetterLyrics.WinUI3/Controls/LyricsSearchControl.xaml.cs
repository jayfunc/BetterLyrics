using BetterLyrics.WinUI3.Helper;
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

        private void ConvertRomajiToKanji(TextBox textBox)
        {
            var selectedText = textBox.SelectedText;
            var selectionStart = textBox.SelectionStart;
            var selectionLength = textBox.SelectionLength;

            var kanji = LanguageHelper.ConvertRomajiToKanji(selectedText);

            textBox.Text = textBox.Text.Remove(selectionStart, selectionLength).Insert(selectionStart, kanji);
            textBox.SelectionStart = selectionStart;
            textBox.SelectionLength = kanji.Length;
        }

        private void PlayLyricsLineButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var lyricsLine = (LyricsLine)((Button)sender).DataContext;
            ViewModel.PlayLyricsLine(lyricsLine);
        }

        private void ConvertMappedAlbumToKanjiMenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ConvertRomajiToKanji(MappedAlbumTextBox);
        }

        private void ConvertMappedArtistToKanjiMenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ConvertRomajiToKanji(MappedArtistTextBox);
        }

        private void ConvertMappedTitleToKanjiMenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ConvertRomajiToKanji(MappedTitleTextBox);
        }
    }
}
