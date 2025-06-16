using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DesktopLyricsPage : Page
    {
        public DesktopLyricsPageViewModel ViewModel => (DesktopLyricsPageViewModel)DataContext;

        public DesktopLyricsSettingsControlViewModel DesktopLyricsSettingsControlViewModel =>
            Ioc.Default.GetService<DesktopLyricsSettingsControlViewModel>()!;

        public DesktopLyricsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<DesktopLyricsPageViewModel>();
        }

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.LimitedLineWidth = e.NewSize.Width;
        }
    }
}
