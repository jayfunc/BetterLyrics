using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsSearchControl : UserControl
{
    public LyricsSearchControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LyricsSearchControlViewModel>();
    }

    public LyricsSearchControlViewModel ViewModel => (LyricsSearchControlViewModel)DataContext;

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

    private void PlayLyricsLineButton_Click(object sender, RoutedEventArgs e)
    {
        var lyricsLine = (LyricsLine)((Button)sender).DataContext;
        ViewModel.PlayLyricsLine(lyricsLine);
    }

    private void ConvertMappedAlbumToKanjiButton_Click(object sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedAlbumTextBox);
    }

    private void ConvertMappedArtistToKanjiButton_Click(object sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedArtistTextBox);
    }

    private void ConvertMappedTitleToKanjiButton_Click(object sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedTitleTextBox);
    }

    public static InfoTagTheme GetInfoTagTheme(bool isIntrinsic)
    {
        return isIntrinsic ? InfoTagTheme.Accent : InfoTagTheme.Default;
    }
}