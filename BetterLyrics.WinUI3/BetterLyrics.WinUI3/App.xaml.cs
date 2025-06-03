using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3 {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application {
        public static App Current => (App)Application.Current;
        public MainWindow? MainWindow { get; private set; }
        public MainWindow? SettingsWindow { get; set; }

        public static ResourceLoader ResourceLoader = new();

        public static DispatcherQueue DispatcherQueue => DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            this.InitializeComponent();
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
                .AddSingleton<SettingsService>()
                .AddSingleton<DatabaseService>()
                // ViewModels
                .AddSingleton<MainViewModel>()
                .AddSingleton<SettingsViewModel>()
                .BuildServiceProvider());

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Activate the window
            MainWindow = new MainWindow();
            MainWindow!.Navigate(typeof(MainPage));
            MainWindow.Activate();
        }

    }
}
