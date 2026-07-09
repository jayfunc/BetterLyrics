using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetterLyrics.Avalonia.Providers;
using BetterLyrics.Avalonia.Services;
using BetterLyrics.Avalonia.Views;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Implementations.Services;
using BetterLyrics.Core.Implementations.Services.FileSystemService;
using BetterLyrics.Core.Implementations.Services.GsmtcService;
using BetterLyrics.Core.Implementations.Services.LyricsSearchService;
using BetterLyrics.Core.Implementations.Services.PluginService;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        PathHelper.EnsureDirectories();
        ConfigureServices();

        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        await InitDatabasesAsync();

        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new NowPlayingWindow(settingsService.AppSettings.WindowBoundsRecords.FirstOrDefault() ?? new());
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new NowPlayingPage();
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            singleViewPlatform.MainView = new NowPlayingPage();

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .WriteTo.File(PathHelper.LogFilePattern, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                // 数据库工厂
                .AddDbContextFactory<PlayHistoryDbContext>(options =>
                    options.UseSqlite($"Data Source={PathHelper.PlayHistoryPath}"))
                .AddDbContextFactory<FilesIndexDbContext>(options =>
                    options.UseSqlite($"Data Source={PathHelper.FilesIndexPath}"))
                .AddDbContextFactory<LyricsCacheDbContext>(options =>
                    options.UseSqlite($"Data Source={PathHelper.LyricsCachePath}"))
                .AddDbContextFactory<SongSearchMapDbContext>(options =>
                    options.UseSqlite($"Data Source={PathHelper.SongSearchMapPath}"))

                // 日志
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })

                // Services
                .AddSingleton<ISettingsService, SettingsService>()
                // .AddSingleton<ISmtcService, SmtcService>()
                .AddSingleton<IGsmtcService, GsmtcService>()
                .AddSingleton<IAlbumArtSearchService, AlbumArtSearchService>()
                .AddSingleton<ILyricsSearchService, LyricsSearchService>()
                .AddSingleton<ITranslationService, TranslationService>()
                .AddSingleton<ITransliterationService, TransliterationService>()
                .AddSingleton<ILastFmService, LastFmService>()
                .AddSingleton<IDiscordService, DiscordService>()
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<IFileSystemService, FileSystemService>()
                .AddSingleton<IPlayHistoryService, PlayHistoryService>()
                .AddSingleton<ILyricsCacheService, LyricsCacheService>()
                .AddSingleton<ISongSearchMapService, SongSearchMapService>()
                .AddSingleton<IPluginService, PluginService>()
                .AddSingleton<IFileWatchService, FileWatchService>()
                .AddSingleton<IAppUpdateService, AppUpdateService>()
                .AddSingleton<INavigationService, NavigationService>()
                .AddSingleton<IAppLifecycleService, AppLifecycleService>()

                // Providers
                .AddSingleton<IPasswordVaultProvider, PasswordVaultProvider>()
                .AddSingleton<IPlatformProvider, PlatformProvider>()
                .AddSingleton<IStringConverterProvider, StringConverterProvider>()
                .AddSingleton<ISystemUIProvider, SystemUIProvider>()
                .AddSingleton<IUniversalMemoryReaderProvider, UniversalMemoryReaderProvider>()
                .AddSingleton<IAppUIThreadProvider, AppUIThreadProvider>()
                .AddSingleton<IGlobalToastProvider, GlobalToastProvider>()
                .AddSingleton<IWindowManagerProvider, WindowManagerProvider>()
                .AddSingleton<IAssetReaderProvider, AssetReaderProvider>()
                .AddSingleton<IMediaManagerProvider, MediaManagerProvider>()
                .AddSingleton<ILauncherProvider, LauncherProvider>()
                .AddSingleton<IFilePickerProvider, FilePickerProvider>()
                .AddSingleton<IProgramProvider, ProgramProvider>()
                .AddSingleton<IMonitorProvider, MonitorProvider>()
                .AddSingleton<ISpoutTextureProvider, SpoutTextureProvider>()

                // ViewModels
                .AddSingleton<AppSettingsControlViewModel>()
                .AddSingleton<PlaybackSettingsControlViewModel>()
                .AddSingleton<MediaSettingsControlViewModel>()
                .AddSingleton<LyricsSearchControlViewModel>()
                .AddSingleton<LyricsWindowSettingsControlViewModel>()
                .AddSingleton<LyricsWindowSwitchControlViewModel>()
                .AddSingleton<LyricsWindowSwitchWindowViewModel>()
                .AddSingleton<SystemTrayViewModel>()
                .AddSingleton<SettingsPageViewModel>()
                .AddSingleton<MusicGalleryPageViewModel>()
                .AddSingleton<AboutControlViewModel>()
                .AddSingleton<StatsDashboardControlViewModel>()
                .AddSingleton<PlayQueueViewModel>()
                .AddSingleton<PluginManagerControlViewModel>()
                .AddSingleton<LyricsSharePageViewModel>()
                .AddSingleton<MusicGalleryWindowViewModel>()
                .AddTransient<NowPlayingPageViewModel>()
                .AddTransient<NowPlayingBarViewModel>()
                .BuildServiceProvider()
        );
    }

    private static async Task InitDatabasesAsync()
    {
        // Init databases
        var playHistoryFactory = Ioc.Default.GetRequiredService<IDbContextFactory<PlayHistoryDbContext>>();
        var songSearchMapFactory = Ioc.Default.GetRequiredService<IDbContextFactory<SongSearchMapDbContext>>();
        var filesIndexFactory = Ioc.Default.GetRequiredService<IDbContextFactory<FilesIndexDbContext>>();
        var lyricsCacheFactory = Ioc.Default.GetRequiredService<IDbContextFactory<LyricsCacheDbContext>>();

        using (var playHistoryDb = await playHistoryFactory.CreateDbContextAsync())
        {
            await playHistoryDb.Database.EnsureCreatedAsync();
        }

        using (var songSearchMapDb = await songSearchMapFactory.CreateDbContextAsync())
        {
            await songSearchMapDb.Database.EnsureCreatedAsync();
        }

        using (var filesIndexDb = await filesIndexFactory.CreateDbContextAsync())
        {
            await filesIndexDb.Database.EnsureCreatedAsync();
        }

        using (var lyricsCacheDb = await lyricsCacheFactory.CreateDbContextAsync())
        {
            await lyricsCacheDb.Database.EnsureCreatedAsync();
        }
    }
}