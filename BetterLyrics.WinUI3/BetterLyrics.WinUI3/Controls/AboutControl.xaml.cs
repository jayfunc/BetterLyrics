using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AboutControl : UserControl
    {
        public AboutControlViewModel ViewModel => (AboutControlViewModel)DataContext;

        public AboutControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
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
