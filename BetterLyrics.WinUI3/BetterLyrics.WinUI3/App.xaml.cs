// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.DiscordService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslationService;
using BetterLyrics.WinUI3.Services.TransliterationService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;
using WinUI3Localizer;

namespace BetterLyrics.WinUI3
{
    public partial class App : Application
    {

        private readonly ILogger<App> _logger;

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

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeLocalizer();

            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            WindowHook.OpenOrShowWindow<SystemTrayWindow>();

            if (settingsService.AppSettings.GeneralSettings.AutoStartLyricsWindow)
            {
                var defaultStatus = settingsService.AppSettings.WindowBoundsRecords.Where(x => x.IsDefault);
                if (defaultStatus != null)
                {
                    foreach (var item in defaultStatus)
                    {
                        WindowHook.OpenOrShowWindow<NowPlayingWindow>(item);
                        if (!settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode)
                        {
                            break;
                        }
                    }
                }
            }
            if (settingsService.AppSettings.MusicGallerySettings.AutoOpen)
            {
                WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
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
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IMediaSessionsService, MediaSessionsService>()
                    .AddSingleton<IAlbumArtSearchService, AlbumArtSearchService>()
                    .AddSingleton<ILyricsSearchService, LyricsSearchService>()
                    .AddSingleton<ILibWatcherService, LibWatcherService>()
                    .AddSingleton<ITranslationService, TranslationService>()
                    .AddSingleton<ITransliterationService, TransliterationService>()
                    .AddSingleton<ILastFMService, LastFMService>()
                    .AddSingleton<IDiscordService, DiscordService>()
                    // ViewModels
                    .AddSingleton<AppSettingsControlViewModel>()
                    .AddSingleton<PlaybackSettingsControlViewModel>()
                    .AddSingleton<MediaSettingsControlViewModel>()
                    .AddSingleton<LyricsSearchControlViewModel>()
                    .AddSingleton<LyricsWindowSettingsControlViewModel>()
                    .AddSingleton<LyricsWindowSwitchControlViewModel>()
                    .AddSingleton<LyricsWindowSwitchWindowViewModel>()
                    .AddSingleton<SettingsWindowViewModel>()
                    .AddSingleton<SystemTrayViewModel>()
                    .AddSingleton<SettingsPageViewModel>()
                    .AddSingleton<MusicGalleryPageViewModel>()
                    .AddSingleton<AboutControlViewModel>()
                    .AddSingleton<MusicGalleryWindowViewModel>()

                    .AddTransient<NowPlayingWindowViewModel>()
                    .AddTransient<NowPlayingPageViewModel>()
                    .AddTransient<NowPlayingBarViewModel>()

                    .BuildServiceProvider()
            );
        }

        private async Task InitializeLocalizer()
        {
            // Initialize a "Strings" folder in the "LocalFolder" for the packaged app.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder stringsFolder = await localFolder.CreateFolderAsync(
              "Strings",
               CreationCollisionOption.OpenIfExists);

            // Create string resources file from app resources if doesn't exists.
            string resourceFileName = "Resources.resw";
            await CreateStringResourceFileIfNotExists(stringsFolder, "de", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "en", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "es", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "fr", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "ja", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "ko", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "ru", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "zh-Hans", resourceFileName);
            await CreateStringResourceFileIfNotExists(stringsFolder, "zh-Hant", resourceFileName);

            ILocalizer localizer = await new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolder.Path)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en";
                })
                .Build();
        }

        private static async Task CreateStringResourceFileIfNotExists(StorageFolder stringsFolder, string language, string resourceFileName)
        {
            StorageFolder languageFolder = await stringsFolder.CreateFolderAsync(
                language,
                CreationCollisionOption.OpenIfExists);

            if (await languageFolder.TryGetItemAsync(resourceFileName) is null)
            {
                string resourceFilePath = Path.Combine(stringsFolder.Name, language, resourceFileName);
                StorageFile resourceFile = await LoadStringResourcesFileFromAppResource(resourceFilePath);
                _ = await resourceFile.CopyAsync(languageFolder);
            }
        }

        private static async Task<StorageFile> LoadStringResourcesFileFromAppResource(string filePath)
        {
            Uri resourcesFileUri = new($"ms-appx:///{filePath}");
            return await StorageFile.GetFileFromApplicationUriAsync(resourcesFileUri);
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
