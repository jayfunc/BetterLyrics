using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public SettingsWindowViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<SettingsWindowViewModel>();

        public SettingsWindow()
        {
            InitializeComponent();
            Title = _resourceService.GetLocalizedString("SettingsPageTitle");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.SetIcons();

            ExtendsContentIntoTitleBar = true;

            AppWindow.Closing += AppWindow_Closing;

            RootFrame.Navigate(typeof(SettingsPage));
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            WindowHelper.CloseWindow<SettingsWindow>();
        }

        private void LyricsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenOrShowWindow<LyricsWindow>();
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenOrShowWindow<MusicGalleryWindow>();
        }
    }
}
