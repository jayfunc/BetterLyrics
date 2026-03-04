using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsShareWindow : Window
    {
        public LyricsShareWindow()
        {
            InitializeComponent();
            this.Init();
        }
    }
}
