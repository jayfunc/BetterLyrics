using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using WinUI3Localizer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3 {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application {
        public static App Current => (App)Application.Current;
        public Window? Window { get; private set; }
        public IThemeService ThemeService { get; set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            this.InitializeComponent();
            ThemeService = new ThemeService();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            // Register services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton(DispatcherQueue.GetForCurrentThread())
                // Services
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IDatabaseService, DatabaseService>()
                // ViewModels
                .AddSingleton<MainViewModel>()
                .AddSingleton<SettingsViewModel>()
                .BuildServiceProvider());

            // Activate the window
            Window = new MainWindow();

            ThemeService.Initialize(Window);
            ThemeService.ConfigureBackdrop(BackdropType.Mica);
            ThemeService.ConfigureElementTheme(ElementTheme.Default);

            Window.Activate();
        }

    }
}
