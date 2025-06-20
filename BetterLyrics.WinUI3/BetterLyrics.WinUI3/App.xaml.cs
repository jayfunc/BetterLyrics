﻿using System.Text;
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

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;

        public static new App Current => (App)Application.Current;

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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Helper.AppInfo.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetService<ILogger<App>>()!;

            UnhandledException += App_UnhandledException;
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
                    .AddSingleton<IPlaybackService, PlaybackService>()
                    .AddSingleton<IMusicSearchService, MusicSearchService>()
                    .AddSingleton<ILibWatcherService, LibWatcherService>()
                    // ViewModels
                    .AddTransient<HostWindowViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<LyricsPageViewModel>()
                    .AddSingleton<LyricsRendererViewModel>()
                    .AddSingleton<LyricsSettingsControlViewModel>()
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
            WindowHelper.OpenLyricsWindow();
        }
    }
}
