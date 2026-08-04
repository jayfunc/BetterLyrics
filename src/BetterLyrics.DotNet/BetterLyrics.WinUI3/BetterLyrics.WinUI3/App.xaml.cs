using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Sdk.Interfaces.Plugins;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using WinUIEx;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace BetterLyrics.WinUI3;

public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private SimpleSplashScreen? _splashScreen;
    private Window? m_window;

    public App()
    {
        InitializeComponent();

        ATL.Settings.NullAbsentValues = true;

        _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

        _logger.LogInformation("App started");

        // 注册全局异常捕获
        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    public new static App Current => (App)Application.Current;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
        var appUiThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

        // 初始化 IAppUIThreadProvider
        m_window = windowManagerProvider.OpenOrShowWindow<SystemTrayWindow>();
        appUiThreadProvider.Initialize(m_window.DispatcherQueue);

        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        if (settingsService.AppSettings.GeneralSettings.ShowSplashScreen)
            _splashScreen = SimpleSplashScreen.ShowDefaultSplashScreen();

        var globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
        globalToastProvider.Initialize();

        await InitAppServicesAsync();

        _splashScreen?.Dispose();

        HandleNormalLaunch();
    }

    private void HandleNormalLaunch()
    {
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

        // 初始化歌词切换窗口
        _ = windowManagerProvider.OpenOrShowWindow<LyricsWindowSwitchWindow>();

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
            windowManagerProvider.OpenOrShowWindow<MusicGalleryWindow>();
    }

    private async Task InitAppServicesAsync()
    {
        // 应用增强动效设置项
        var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        UpdateGlobalStyles(settingsService.AppSettings.GeneralSettings
            .EnhanceControlInteractiveAnimations);

        // 迁移逻辑
        var migrationService = Ioc.Default.GetRequiredService<IDatabaseMigrationService>();
        await migrationService.MigrateAllAsync();

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
                        Provider = (LyricsProvider)plugin.Id,
                        IsEnabled = true
                    });
        }
    }

    private void UpdateGlobalStyles(bool useCustom)
    {
        var mergedDicts = Application.Current.Resources.MergedDictionaries;

        var fluentDict = mergedDicts.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("FluentStyles.xaml"));
        var defaultDict = mergedDicts.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("DefaultStyles.xaml"));

        if (useCustom)
        {
            if (fluentDict == null)
                mergedDicts.Add(new ResourceDictionary
                { Source = new Uri("ms-appx:///Themes/FluentStyles.xaml") });

            if (defaultDict != null) mergedDicts.Remove(defaultDict);
        }
        else
        {
            if (defaultDict == null)
                mergedDicts.Add(new ResourceDictionary
                { Source = new Uri("ms-appx:///Themes/DefaultStyles.xaml") });

            if (fluentDict != null) mergedDicts.Remove(fluentDict);
        }
    }


    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "App_UnhandledException");
        e.Handled = true;
    }

    private void CurrentDomain_FirstChanceException(object? sender,
        FirstChanceExceptionEventArgs e)
    {
        // FirstChance 异常非常多（比如内部 try-catch 也会触发），通常建议只在 Debug 模式记录，或者过滤特定类型
        // _logger.LogError(e.Exception, "CurrentDomain_FirstChanceException"); 
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