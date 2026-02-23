using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AppSettingsControl : UserControl
    {
        public AppSettingsControlViewModel ViewModel => (AppSettingsControlViewModel)DataContext;

        public AppSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<AppSettingsControlViewModel>();
        }

        private void AutoStartupToggleSwitch_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _ = ViewModel.ToggleAutoStartupAsync(AutoStartupToggleSwitch.IsOn);
        }
    }
}
