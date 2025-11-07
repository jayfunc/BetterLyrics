// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3
{
    public partial class App : Application
    {

        private readonly ILogger<App> _logger;
        private readonly ISettingsService _settingsService;

        public static new App Current => (App)Application.Current;

        private static Mutex? _instanceMutex;

        public App()
        {
            this.InitializeComponent();

            EnsureSingleInstance();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PathHelper.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
            _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void EnsureSingleInstance()
        {
            _instanceMutex = new Mutex(true, Constants.App.AppName, out bool createdNew);

            if (!createdNew)
            {
                User32.MessageBox(HWND.NULL, new ResourceLoader().GetString("TryRunMultipleInstance"), null, User32.MB_FLAGS.MB_APPLMODAL);
                Environment.Exit(0);
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            WindowHelper.OpenOrShowWindow<LyricsWindow>();
            if (_settingsService.AppSettings.MusicGallerySettings.AutoOpen)
            {
                WindowHelper.OpenOrShowWindow<MusicGalleryWindow>();
            }
        }

        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.File(PathHelper.LogFilePattern, rollingInterval: RollingInterval.Day)
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
                    .AddSingleton<ILiveStatesService, LiveStatesService>()
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IMediaSessionsService, MediaSessionsService>()
                    .AddSingleton<IAlbumArtSearchService, AlbumArtSearchService>()
                    .AddSingleton<ILyricsSearchService, LyricsSearchService>()
                    .AddSingleton<ILibWatcherService, LibWatcherService>()
                    .AddSingleton<ITranslateService, TranslateService>()
                    .AddSingleton<ILastFMService, LastFMService>()
                    .AddSingleton<IResourceService, ResourceService>()
                    // ViewModels
                    .AddSingleton<AppSettingsControlViewModel>()
                    .AddSingleton<PlaybackSettingsControlViewModel>()
                    .AddSingleton<MediaSettingsControlViewModel>()
                    .AddSingleton<LyricsSearchControlViewModel>()
                    .AddSingleton<LyricsWindowSettingsControlViewModel>()
                    .AddSingleton<LyricsWindowSwitchControlViewModel>()
                    .AddSingleton<LyricsWindowSwitchWindowViewModel>()
                    .AddSingleton<LyricsWindowViewModel>()
                    .AddSingleton<SettingsWindowViewModel>()
                    .AddSingleton<SystemTrayViewModel>()
                    .AddSingleton<SettingsPageViewModel>()
                    .AddSingleton<LyricsPageViewModel>()
                    .AddSingleton<MusicGalleryViewModel>()
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
