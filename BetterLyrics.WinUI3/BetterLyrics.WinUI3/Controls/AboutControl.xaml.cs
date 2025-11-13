using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

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
    }
}
