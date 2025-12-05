using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public SettingsWindowViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<SettingsWindowViewModel>();

        public SettingsWindow()
        {
            InitializeComponent();

            this.Init("SettingsPageTitle");

            AppWindow.Closing += AppWindow_Closing;

            RootFrame.Navigate(typeof(SettingsPage));
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            this.CloseWindow();
        }

        private void LyricsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<NowPlayingWindow>();
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }
    }
}
