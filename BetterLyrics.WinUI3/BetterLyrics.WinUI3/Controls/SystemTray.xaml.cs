using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class SystemTray : UserControl
    {
        public SystemTrayViewModel ViewModel => (SystemTrayViewModel)DataContext;

        public SystemTray()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<SystemTrayViewModel>();
        }
    }
}
