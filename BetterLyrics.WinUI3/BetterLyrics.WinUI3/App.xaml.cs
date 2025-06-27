// 2025/6/23 by Zhe Fang

using System;
using System.Text;
using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class
    /// </summary>
    public partial class App : Application
    {
        #region Fields

        /// <summary>
        /// Defines the _logger
        /// </summary>
        private readonly ILogger<App> _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            DispatcherQueueTimer = DispatcherQueue.CreateTimer();
            ResourceLoader = new ResourceLoader();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppInfo.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetService<ILogger<App>>()!;

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Current
        /// </summary>
        public static new App Current => (App)Application.Current;

        /// <summary>
        /// Gets the DispatcherQueue
        /// </summary>
        public static DispatcherQueue? DispatcherQueue { get; private set; }

        /// <summary>
        /// Gets the DispatcherQueueTimer
        /// </summary>
        public static DispatcherQueueTimer? DispatcherQueueTimer { get; private set; }

        /// <summary>
        /// Gets the ResourceLoader
        /// </summary>
        public static ResourceLoader? ResourceLoader { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Invoked when the application is launched
        /// </summary>
        /// <param name="args">Details about the launch request and process</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            WindowHelper.OpenLyricsWindow();
        }

        /// <summary>
        /// The ConfigureServices
        /// </summary>
        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(AppInfo.LogFilePattern, rollingInterval: RollingInterval.Day)
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
                    .AddSingleton<IPlaybackService, PlaybackService>()
                    .AddSingleton<IMusicSearchService, MusicSearchService>()
                    .AddSingleton<ILibWatcherService, LibWatcherService>()
                    // ViewModels
                    .AddTransient<HostWindowViewModel>()
                    .AddSingleton<SystemTrayViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<LyricsPageViewModel>()
                    .AddSingleton<LyricsRendererViewModel>()
                    .AddSingleton<LyricsSettingsControlViewModel>()
                    .BuildServiceProvider()
            );
        }

        /// <summary>
        /// The App_UnhandledException
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="Microsoft.UI.Xaml.UnhandledExceptionEventArgs"/></param>
        private void App_UnhandledException(
            object sender,
            Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            _logger.LogError(e.Exception, "App_UnhandledException");
            e.Handled = true;
        }

        /// <summary>
        /// The CurrentDomain_FirstChanceException
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs"/></param>
        private void CurrentDomain_FirstChanceException(
            object? sender,
            System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e
        )
        {
            _logger.LogError(e.Exception, "TaskScheduler_UnobservedTaskException");
        }

        /// <summary>
        /// The CurrentDomain_UnhandledException
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="System.UnhandledExceptionEventArgs"/></param>
        private void CurrentDomain_UnhandledException(
            object sender,
            System.UnhandledExceptionEventArgs e
        )
        {
            _logger.LogError(e.ExceptionObject.ToString(), "CurrentDomain_UnhandledException");
        }

        /// <summary>
        /// The TaskScheduler_UnobservedTaskException
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/></param>
        /// <param name="e">The e<see cref="UnobservedTaskExceptionEventArgs"/></param>
        private void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e
        )
        {
            //_logger.LogError(e.Exception, "TaskScheduler_UnobservedTaskException");
        }

        #endregion
    }
}
