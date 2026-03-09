using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.DbContext;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.AppUpdateService;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.FileWatchService;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace BetterLyrics.WinUI3
{
    public partial class App : Application
    {
        private Window? m_window;
        private readonly ILogger<App> _logger;
        public static new App Current => (App)Application.Current;
        public static Window SystemTrayWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();

            _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

            // 注册全局异常捕获
            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 必须，加上此行以防止 SyncTheme 时线程被阻塞（原因未明）
            _ = Ioc.Default.GetRequiredService<ISettingsService>();

            var splashWindow = WindowHook.OpenOrShowWindow<SplashWindow>();
            GlobalToastManager.Initialize();

            await Task.Delay(100);
            await InitAppServicesAsync();

            HandleNormalLaunch();

            WindowHook.CloseWindow(splashWindow);
        }

        private void HandleNormalLaunch()
        {
            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            // 初始化系统托盘
            m_window = WindowHook.OpenOrShowWindow<SystemTrayWindow>();
            SystemTrayWindow = m_window;

            // 自动打开歌词窗口逻辑
            if (settingsService.AppSettings.GeneralSettings.AutoStartLyricsWindow)
            {
                var defaultStatus = settingsService.AppSettings.WindowBoundsRecords.Where(x => x.IsDefault);
                if (defaultStatus != null)
                {
                    foreach (var item in defaultStatus)
                    {
                        WindowHook.OpenOrShowWindow<NowPlayingWindow>(item);
                        if (!settingsService.AppSettings.GeneralSettings.MultiNowPlayingWindowMode) break;
                    }
                }
            }

            // 自动打开音乐库逻辑
            if (settingsService.AppSettings.MusicGallerySettings.AutoOpen)
            {
                WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
            }
        }

        private async Task InitAppServicesAsync()
        {
            await InitDatabasesAsync();

            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            // 应用增强动效设置项
            settingsService.UpdateGlobalStyles(settingsService.AppSettings.GeneralSettings.EnhanceControlInteractiveAnimations);

            // 迁移逻辑
            var songSearchMapService = Ioc.Default.GetRequiredService<ISongSearchMapService>();
            var obsoleteSongSearchMap = settingsService.AppSettings.MappedSongSearchQueries;
            if (obsoleteSongSearchMap.Count > 0)
            {
                foreach (var item in obsoleteSongSearchMap)
                {
                    await songSearchMapService.SaveMappingAsync(item);
                }
                obsoleteSongSearchMap.Clear();
            }

            // 启动后台扫描
            var fileSystemService = Ioc.Default.GetRequiredService<IFileSystemService>();
            foreach (var item in settingsService.AppSettings.LocalMediaFolders)
            {
                if (item.LastSyncTime == null)
                {
                    _ = Task.Run(async () => await fileSystemService.ScanMediaFolderAsync(item, token: CancellationToken.None));
                }
            }
            fileSystemService.StartAllFolderTimers();

            // 实时扫描
            _ = Ioc.Default.GetRequiredService<IFileWatchService>();

            // 加载插件
            var pluginService = Ioc.Default.GetRequiredService<IPluginService>();
            await pluginService.LoadPluginsAsync();

            // 确保播放源配置内歌词源与插件保持最新
            EnsureLyricsSearchProvidersInfo();

            // 预加载系统字体列表
            await FontHelper.GetSystemFontFamiliesAsync();

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
                    Id = (int)pluginService.GetPluginHashedId(p.Id)
                })
                .ToList();

            var validPluginIds = validPluginData.Select(x => x.Id).ToHashSet();

            foreach (var providerInfo in settingsService.AppSettings.MediaSourceProvidersInfo)
            {
                var targetList = providerInfo.LyricsSearchProvidersInfo;

                var itemsToRemove = targetList
                    .Where(item => (int)item.Provider >= 1000 && !validPluginIds.Contains((int)item.Provider))
                    .ToList();

                foreach (var item in itemsToRemove)
                {
                    targetList.Remove(item);
                }

                var existingIds = targetList.Select(x => (int)x.Provider).ToHashSet();

                foreach (var plugin in validPluginData)
                {
                    if (!existingIds.Contains(plugin.Id))
                    {
                        targetList.Add(new LyricsSearchProviderInfo
                        {
                            Provider = (LyricsSearchProvider)plugin.Id,
                            IsEnabled = true
                        });
                    }
                }
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "App_UnhandledException");
            e.Handled = true;
        }

        private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
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
}