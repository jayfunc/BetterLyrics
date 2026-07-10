using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetterLyrics.Avalonia.Providers;
using BetterLyrics.Avalonia.Services;
using BetterLyrics.Avalonia.Views;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Implementations.Services;
using BetterLyrics.Core.Implementations.Services.FileSystemService;
using BetterLyrics.Core.Implementations.Services.GsmtcService;
using BetterLyrics.Core.Implementations.Services.LyricsSearchService;
using BetterLyrics.Core.Implementations.Services.PluginService;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.Sdk.Interfaces.Plugins;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia;

/// <summary>
/// Splash window workaround ref: https://github.com/kivarsen/AvaloniaSplashScreenDemo/blob/master/AvaloniaSplashScreenDemo/App.axaml.cs
/// </summary>
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashWindow = new SplashWindow();
            desktop.MainWindow = splashWindow;
            splashWindow.Show();

            // Do necessary work when showing splash window
            await InitAppServicesAsync();

            // Create the main window, and swap it in for the real main window
            // before closeing splash window
            var systemTrayWindow = new SystemTrayWindow();
            desktop.MainWindow = systemTrayWindow;
            systemTrayWindow.Show();

            SetupSystemTray();

            // Get rid of the splash screen
            splashWindow.Close();

            HandleNormalLaunch();
        }

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
#if WINDOWS
                .AddSingleton<ISmtcService, SmtcService>()
#endif
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
#if WINDOWS
                .AddSingleton<IMediaManagerProvider, MediaManagerProvider>()
#endif
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

    private void SetupSystemTray()
    {
        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons != null && trayIcons.Count > 0)
        {
            var trayIcon = trayIcons[0];
            var sysTrayVm = Ioc.Default.GetRequiredService<SystemTrayViewModel>();

            trayIcon.Command = sysTrayVm.TrayIconClickedCommand;

            var menu = new NativeMenu();

            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTraySwitch_Text) { Command = sysTrayVm.NavigationService.OpenLyricsWindowSwitchWindowCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTraySearch_Text) { Command = sysTrayVm.NavigationService.OpenLyricsSearchWindowCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTrayMusicGallery_Text) { Command = sysTrayVm.NavigationService.OpenMusicGalleryWindowCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTrayStats_Text) { Command = sysTrayVm.NavigationService.OpenStatsDashboardWindowCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.LyricsPageLyricsCard_Text) { Command = sysTrayVm.NavigationService.OpenLyricsShareWindowCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTraySettings_Text) { Command = sysTrayVm.NavigationService.OpenSettingsWindowCommand });

            menu.Items.Add(new NativeMenuItemSeparator());

            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTrayRestart_Text) { Command = sysTrayVm.AppLifecycleService.RestartAppCommand });
            menu.Items.Add(new NativeMenuItem(Strings.Resources.SystemTrayExit_Text) { Command = sysTrayVm.AppLifecycleService.ExitAppCommand });

            trayIcon.Menu = menu;
        }
    }

    private void HandleNormalLaunch()
    {
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

        // 初始化歌词切换窗口
        _ = windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);

        // 自动打开歌词窗口逻辑
        if (settingsService.AppSettings.GeneralSettings.AutoStartLyricsWindow)
        {
            var defaultStatus = settingsService.AppSettings.WindowBoundsRecords.Where(x => x.IsDefault);
            if (defaultStatus != null)
                foreach (var item in defaultStatus)
                {
                    windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(item);
                    if (!settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode) break;
                }
        }

        // 自动打开音乐库逻辑
        if (settingsService.AppSettings.MusicGallerySettings.AutoOpen)
            windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
    }

    private async Task InitAppServicesAsync()
    {
        await InitDatabasesAsync();

        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        // 应用增强动效设置项
        UpdateGlobalStyles(settingsService.AppSettings.GeneralSettings
            .EnhanceControlInteractiveAnimations);

        // 迁移逻辑
        var songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();
        var obsoleteSongSearchMap = settingsService.AppSettings.MappedSongSearchQueries;
        if (obsoleteSongSearchMap.Count > 0)
        {
            foreach (var item in obsoleteSongSearchMap) await songSearchMapService.SaveMappingAsync(item);

            obsoleteSongSearchMap.Clear();
        }

        // 启动后台扫描
        var fileSystemService = Ioc.Default.GetRequiredService<IFileSystemService>();
        foreach (var item in settingsService.AppSettings.LocalMediaFolders)
            if (item.LastSyncTime == null)
                _ = Task.Run(async () =>
                    await fileSystemService.ScanMediaFolderAsync(item, token: CancellationToken.None));

        fileSystemService.StartAllFolderTimers();

        // 实时扫描
        _ = Ioc.Default.GetRequiredService<IFileWatchService>();

        // 加载插件
        var pluginService = Ioc.Default.GetRequiredService<IPluginService>();
        await pluginService.LoadPluginsAsync();

        // 确保播放源配置内歌词源与插件保持最新
        EnsureLyricsSearchProvidersInfo();

        // 启动周期更新检测
        var appUpdateService = Ioc.Default.GetRequiredService<IAppUpdateService>();
        appUpdateService.StartDailyCheck();
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

    private void EnsureLyricsSearchProvidersInfo()
    {
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        var pluginService = Ioc.Default.GetRequiredService<IPluginService>();

        var validPluginData = settingsService.AppSettings.PluginsInfo
            .Where(x => x.Plugin is ILyricsSource)
            .Select(p => new
            {
                Id = pluginService.GetPluginHashedId(p.Id)
            })
            .ToList();

        var validPluginIds = validPluginData.Select(x => x.Id).ToHashSet();

        foreach (var providerInfo in settingsService.AppSettings.MediaSourceProvidersInfo)
        {
            var targetList = providerInfo.LyricsSearchProvidersInfo;

            var itemsToRemove = targetList
                .Where(item => (int)item.Provider >= 1000 && !validPluginIds.Contains((int)item.Provider))
                .ToList();

            foreach (var item in itemsToRemove) targetList.Remove(item);

            var existingIds = targetList.Select(x => (int)x.Provider).ToHashSet();

            foreach (var plugin in validPluginData)
                if (!existingIds.Contains(plugin.Id))
                    targetList.Add(new LyricsSearchProviderInfo
                    {
                        Provider = (LyricsSearchProvider)plugin.Id,
                        IsEnabled = true
                    });
        }
    }

    private void UpdateGlobalStyles(bool useCustom)
    {
        // TODO
        //var mergedDicts = Application.Current.Resources.MergedDictionaries;

        //var fluentDict = mergedDicts.FirstOrDefault(d =>
        //    d.Source != null && d.Source.OriginalString.Contains("FluentStyles.xaml"));
        //var defaultDict = mergedDicts.FirstOrDefault(d =>
        //    d.Source != null && d.Source.OriginalString.Contains("DefaultStyles.xaml"));

        //if (useCustom)
        //{
        //    if (fluentDict == null)
        //        mergedDicts.Add(new ResourceDictionary
        //        { Source = new Uri("ms-appx:///Themes/FluentStyles.xaml") });

        //    if (defaultDict != null) mergedDicts.Remove(defaultDict);
        //}
        //else
        //{
        //    if (defaultDict == null)
        //        mergedDicts.Add(new ResourceDictionary
        //        { Source = new Uri("ms-appx:///Themes/DefaultStyles.xaml") });

        //    if (fluentDict != null) mergedDicts.Remove(fluentDict);
        //}
    }
}