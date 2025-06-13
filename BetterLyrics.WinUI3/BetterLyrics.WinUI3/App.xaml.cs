using System.Text;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;

        public static new App Current => (App)Application.Current;
        public HostWindow? MainWindow { get; set; }
        public HostWindow? SettingsWindow { get; set; }
        public OverlayWindow? OverlayWindow { get; set; }

        public static ResourceLoader? ResourceLoader { get; private set; }

        public static DispatcherQueue? DispatcherQueue { get; private set; }
        public static DispatcherQueueTimer? DispatcherQueueTimer { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            DispatcherQueueTimer = DispatcherQueue.CreateTimer();
            ResourceLoader = new ResourceLoader();

            Helper.AppInfo.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetService<ILogger<App>>()!;

            // UnhandledException += App_UnhandledException;
        }

        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Helper.AppInfo.LogFilePattern, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Register services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog();
                    })
                    // Services
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IDatabaseService, DatabaseService>()
                    .AddSingleton<IPlaybackService, PlaybackService>()
                    // Renderer
                    .AddSingleton<AlbumArtRenderer>()
                    .AddSingleton<InAppLyricsRenderer>()
                    .AddSingleton<DesktopLyricsRenderer>()
                    // ViewModels
                    .AddSingleton<HostViewModel>()
                    .AddSingleton<AlbumArtViewModel>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<BaseSettingsViewModel>()
                    .AddSingleton<GlobalViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<InAppLyricsViewModel>()
                    .AddSingleton<DesktopLyricsViewModel>()
                    .AddSingleton<AlbumArtOverlayViewModel>()
                    .BuildServiceProvider()
            );
        }

        private void App_UnhandledException(
            object sender,
            Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            _logger.LogError(e.Exception, "App_UnhandledException");
            e.Handled = true;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var overlayWindow = new OverlayWindow();
            overlayWindow.Navigate(typeof(DesktopLyricsPage));
            overlayWindow.Hide();
            App.Current.OverlayWindow = overlayWindow;

            // Activate the window
            MainWindow = new HostWindow();
            MainWindow!.Navigate(typeof(MainPage));
            MainWindow.Activate();
        }
    }
}
