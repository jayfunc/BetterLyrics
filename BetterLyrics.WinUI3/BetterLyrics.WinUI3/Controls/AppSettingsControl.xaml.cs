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

        private async void AutoStartupToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.IsOn = await ViewModel.DetectIsAutoStartupEnabledAsync();
            AutoStartupToggleSwitch.Toggled += AutoStartupToggleSwitch_Toggled;
        }

        private void AutoStartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutoStartupAsync(AutoStartupToggleSwitch.IsOn);
        }

        private void AutoStartupToggleSwitch_Unloaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.Toggled -= AutoStartupToggleSwitch_Toggled;
        }
    }
}
