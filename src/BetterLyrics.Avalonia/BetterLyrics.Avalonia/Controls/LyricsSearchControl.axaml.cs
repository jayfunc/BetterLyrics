using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class LyricsSearchControl : UserControl
{
    public LyricsSearchControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LyricsSearchControlViewModel>();
    }

    public LyricsSearchControlViewModel ViewModel => (LyricsSearchControlViewModel)DataContext!;

    private void ConvertRomajiToKanji(TextBox textBox)
    {
        var selectedText = textBox.SelectedText;
        var selectionStart = textBox.SelectionStart;
        var selectionEnd = textBox.SelectionEnd;

        if (string.IsNullOrEmpty(selectedText)) return;

        var kanji = LanguageHelper.ConvertRomajiToKanji(selectedText);

        textBox.Text = textBox.Text?.Remove(selectionStart, selectedText.Length).Insert(selectionStart, kanji);
        textBox.SelectionStart = selectionStart;
        textBox.SelectionEnd = selectionStart + kanji.Length;
    }

    private void PlayLyricsLineButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LyricsLine lyricsLine })
        {
            ViewModel.PlayLyricsLine(lyricsLine);
        }
    }

    private void ConvertMappedAlbumToKanjiButton_Click(object? sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedAlbumTextBox);
    }

    private void ConvertMappedArtistToKanjiButton_Click(object? sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedArtistTextBox);
    }

    private void ConvertMappedTitleToKanjiButton_Click(object? sender, RoutedEventArgs e)
    {
        ConvertRomajiToKanji(MappedTitleTextBox);
    }
}
