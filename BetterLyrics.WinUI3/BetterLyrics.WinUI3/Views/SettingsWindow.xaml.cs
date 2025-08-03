using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsWindow : Window
    {
        public SettingsWindowViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<SettingsWindowViewModel>();

        public SettingsWindow()
        {
            InitializeComponent();
            Title = App.ResourceLoader?.GetString("SettingsPageTitle");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.SetIcons();

            RootFrame.Navigate(typeof(SettingsPage));
        }

        private void TipContainerCenter_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.SettingsWindowNotificationPanel = TipContainerCenter;
        }
    }
}
