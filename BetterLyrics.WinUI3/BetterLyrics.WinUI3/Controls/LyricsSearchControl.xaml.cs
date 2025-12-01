using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

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

        private void ArtistsSplitHintRichTextBlock_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is RichTextBlock richTextBlock)
            {
                TextHighlighter highlighter = new()
                {
                    Background = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush,
                    Ranges =
                    {
                        new() { StartIndex = 0, Length = 1 },
                        new() { StartIndex = 5, Length = 1 },
                        new() { StartIndex = 10, Length = 1 },
                        new() { StartIndex = 15, Length = 1 },
                        new() { StartIndex = 20, Length = 1 },
                        new() { StartIndex = 25, Length = 1 },
                    }
                };
                richTextBlock.TextHighlighters.Add(highlighter);
            }
        }
    }
}
