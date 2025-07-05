// 2025/6/23 by Zhe Fang

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace BetterLyrics.WinUI3
{
    public partial class App : Application
    {

        private readonly ILogger<App> _logger;
        private readonly ISettingsService _settingsService;

        public static new App Current => (App)Application.Current;
        public static DispatcherQueue? DispatcherQueue { get; private set; }
        public static DispatcherQueueTimer? DispatcherQueueTimer { get; private set; }
        public static ResourceLoader? ResourceLoader { get; private set; }

        public App()
        {
            this.InitializeComponent();

            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            DispatcherQueueTimer = DispatcherQueue.CreateTimer();
            ResourceLoader = new ResourceLoader();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AppInfo.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
            _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            WindowHelper.OpenOrShowWindow<LyricsWindow>();
            var lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow == null) return;

            string[] commandLineArguments = Environment.GetCommandLineArgs();
            if (commandLineArguments.Length > 1)
            {
                commandLineArguments = commandLineArguments.Skip(1).ToArray();
                if (commandLineArguments.First() == AppInfo.UnlockWindowTag)
                {
                    lyricsWindow.AutoSelectLyricsMode(AutoStartWindowType.DesktopMode, false);
                    return;
                }
            }
            lyricsWindow.AutoSelectLyricsMode();
        }

        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
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
                    .AddSingleton<LyricsWindowViewModel>()
                    .AddSingleton<SettingsWindowViewModel>()
                    .AddSingleton<SystemTrayViewModel>()
                    .AddSingleton<SettingsPageViewModel>()
                    .AddSingleton<LyricsPageViewModel>()
                    .AddSingleton<LyricsRendererViewModel>()
                    .BuildServiceProvider()
            );
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "App_UnhandledException");
            e.Handled = true;
        }

        private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "CurrentDomain_FirstChanceException");
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.ExceptionObject.ToString(), "CurrentDomain_UnhandledException");
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "TaskScheduler_UnobservedTaskException");
        }
    }
}
