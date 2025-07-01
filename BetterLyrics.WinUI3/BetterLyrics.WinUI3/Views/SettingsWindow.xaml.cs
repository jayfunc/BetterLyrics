using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            Title = App.ResourceLoader!.GetString("SettingsPageTitle");
            ExtendsContentIntoTitleBar = true;

            AppWindow.Closing += AppWindow_Closing;
        }

        public SettingsWindowViewModel ViewModel { get; set; } =
            Ioc.Default.GetRequiredService<SettingsWindowViewModel>();

        private void AppWindow_Closing(
            Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args
        )
        {
            args.Cancel = true; // Prevent the window from closing
            this.Hide(true);
        }
    }
}
