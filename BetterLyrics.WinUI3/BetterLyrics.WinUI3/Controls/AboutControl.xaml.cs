using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AboutControl : UserControl
    {
        private bool _isCreditsScrolling = false;
        public AboutControlViewModel ViewModel => (AboutControlViewModel)DataContext;

        public AboutControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
        }

        private async void Patron_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            CreditsReel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            CreditsReel.Opacity = 1;
            _isCreditsScrolling = true;
        }

        private void CompositionTarget_Rendering(object? sender, object e)
        {
            if (_isCreditsScrolling)
            {
                CreditsReelScrollViewer.ChangeView(null, CreditsReelScrollViewer.VerticalOffset + 0.5, null);
            }
        }

        private async void CreditsReel_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CreditsReel.Opacity = 0;
            await Task.Delay(Constants.Time.AnimationDuration);
            CreditsReel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CreditsReelScrollViewer.ChangeView(null, 0, null);
        }

        private void CreditsReel_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            CreditsReelHeader.LineHeight = e.NewSize.Height;
            CreditsReelFooter.LineHeight = e.NewSize.Height / 2;
        }

        private void RichTextBlock_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isCreditsScrolling = false;
        }

        private void RichTextBlock_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isCreditsScrolling = true;
        }

        private void WeChat_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            WeChatFlyout.ShowAt(WeChatButton);
        }

        private void AlipayButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            AlipayFlyout.ShowAt(AlipayButton);
        }
    }
}
