using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.DbContext;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.DiscordService;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.LyricsCacheService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.PlayHistoryService;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SMTCService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using BetterLyrics.WinUI3.Services.TranslationService;
using BetterLyrics.WinUI3.Services.TransliterationService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle; // 关键：App生命周期管理
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3
{
    public partial class App : Application
    {
        private Window? m_window;
        private readonly ILogger<App> _logger;
        public static new App Current => (App)Application.Current;

        private readonly string _appKey = Windows.ApplicationModel.Package.Current.Id.FamilyName;

        public App()
        {
            // Must be done before InitializeComponent
            if (!TryHandleSingleInstance())
            {
                // 如果移交成功直接退出当前进程
                Environment.Exit(0);
                return;
            }

            this.InitializeComponent();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            PathHelper.EnsureDirectories();
            ConfigureServices();

            _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

            // 注册全局异常捕获
            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// 处理单实例逻辑。
        /// 返回 true 表示我是主实例，继续运行。
        /// 返回 false 表示我是第二个实例，已通知主实例，我应该退出。
        /// </summary>
        private bool TryHandleSingleInstance()
        {
            // 尝试查找或注册当前实例
            var mainInstance = AppInstance.FindOrRegisterForKey(_appKey);

            // 如果当前实例就是注册的那个主实例
            if (mainInstance.IsCurrent)
            {
                // 监听 "Activated" 事件。
                // 当第二个实例启动并重定向过来时，这个事件会被触发。
                mainInstance.Activated += OnMainInstanceActivated;
                return true;
            }
            else
            {
                // 我不是主实例，我是后来者。
                // 获取当前实例的激活参数（比如是通过文件双击打开的，这里能拿到文件路径）
                var args = AppInstance.GetCurrent().GetActivatedEventArgs();

                // 将激活请求重定向给主实例
                // 注意：这里是同步等待，确保发送成功后再退出
                try
                {
                    mainInstance.RedirectActivationToAsync(args).AsTask().Wait();
                }
                catch (Exception)
                {
                    // 即使重定向失败，作为第二个实例也应该退出
                }

                return false;
            }
        }

        /// <summary>
        /// 当第二个实例试图启动时，主实例会收到此回调
        /// </summary>
        private void OnMainInstanceActivated(object? sender, AppActivationArguments e)
        {
            // 这个事件是在后台线程触发的，必须切回 UI 线程操作窗口
            m_window?.DispatcherQueue.TryEnqueue(() =>
            {
                HandleActivation();
            });
        }

        /// <summary>
        /// 唤醒逻辑
        /// </summary>
        private void HandleActivation()
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitDatabasesAsync();

            var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

            // Migrate MappedSongSearchQueries
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

            // Start scan tasks in background
            var fileSystemService = Ioc.Default.GetRequiredService<IFileSystemService>();

            foreach (var item in settingsService.AppSettings.LocalMediaFolders)
            {
                if (item.LastSyncTime == null)
                {
                    _ = Task.Run(async () => await fileSystemService.ScanMediaFolderAsync(item, CancellationToken.None));
                }
            }
            fileSystemService.StartAllFolderTimers();

            // Ensure plugins
            var pluginService = Ioc.Default.GetRequiredService<IPluginService>();
            pluginService.LoadPlugins();

            // Init system tray
            m_window = WindowHook.OpenOrShowWindow<SystemTrayWindow>();

            // Open lyrics window if set
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

            // Open music gallery if set
            if (settingsService.AppSettings.MusicGallerySettings.AutoOpen)
            {
                WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
            }
        }

        private async Task InitDatabasesAsync()
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

        private static void ConfigureServices()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
                .WriteTo.File(PathHelper.LogFilePattern, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // 数据库工厂
                    .AddDbContextFactory<PlayHistoryDbContext>(options => options.UseSqlite($"Data Source={PathHelper.PlayHistoryPath}"))
                    .AddDbContextFactory<FilesIndexDbContext>(options => options.UseSqlite($"Data Source={PathHelper.FilesIndexPath}"))
                    .AddDbContextFactory<LyricsCacheDbContext>(options => options.UseSqlite($"Data Source={PathHelper.LyricsCachePath}"))
                    .AddDbContextFactory<SongSearchMapDbContext>(options => options.UseSqlite($"Data Source={PathHelper.SongSearchMapPath}"))

                    // 日志
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog();
                    })

                    // Services
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<ISMTCService, SMTCService>()
                    .AddSingleton<IGSMTCService, GSMTCService>()
                    .AddSingleton<IAlbumArtSearchService, AlbumArtSearchService>()
                    .AddSingleton<ILyricsSearchService, LyricsSearchService>()
                    .AddSingleton<ITranslationService, TranslationService>()
                    .AddSingleton<ITransliterationService, TransliterationService>()
                    .AddSingleton<ILastFMService, LastFMService>()
                    .AddSingleton<IDiscordService, DiscordService>()
                    .AddSingleton<ILocalizationService, LocalizationService>()
                    .AddSingleton<IFileSystemService, FileSystemService>()
                    .AddSingleton<IPlayHistoryService, PlayHistoryService>()
                    .AddSingleton<ILyricsCacheService, LyricsCacheService>()
                    .AddSingleton<ISongSearchMapService, SongSearchMapService>()
                    .AddSingleton<IPluginService, PluginService>()

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
                    .AddSingleton<StatsDashboardControlViewModel>()
                    .AddSingleton<PlayQueueViewModel>()

                    .AddTransient<NowPlayingWindowViewModel>()
                    .AddTransient<NowPlayingPageViewModel>()
                    .AddTransient<NowPlayingBarViewModel>()

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