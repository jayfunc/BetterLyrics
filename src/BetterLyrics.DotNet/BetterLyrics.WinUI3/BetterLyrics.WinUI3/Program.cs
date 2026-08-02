// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Implementations.Services;
using BetterLyrics.Core.Implementations.Services.FileSystemService;
using BetterLyrics.Core.Implementations.Services.GsmtcService;
using BetterLyrics.Core.Implementations.Services.LyricsSearchService;
using BetterLyrics.Core.Implementations.Services.PluginService;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using BetterLyrics.Core.ViewModels;
using WinRT;
using BetterLyrics.Core.ViewModels.MusicGalleryPageViewModel;

namespace BetterLyrics.WinUI3;

public class Program
{
    private static ILogger<Program>? _logger;

    private static IntPtr redirectEventHandle = IntPtr.Zero;

    [STAThread]
    private static int Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();
        var isRedirect = DecideRedirection();

        if (!isRedirect)
            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                PathHelper.EnsureDirectories();

                ConfigureServices();

                _logger = Ioc.Default.GetRequiredService<ILogger<Program>>();

                _ = new App();

                var args = AppInstance.GetCurrent().GetActivatedEventArgs();
                HandleActivation(args, true);
            });

        return 0;
    }

    private static bool DecideRedirection()
    {
        var isRedirect = false;
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var kind = args.Kind;
        var keyInstance = AppInstance.FindOrRegisterForKey("MySingleInstanceApp");

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
        }
        else
        {
            isRedirect = true;
            RedirectActivationTo(args, keyInstance);
        }

        return isRedirect;
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        var appUiThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();
        appUiThreadProvider.Execute(() => { HandleActivation(args); });
    }

    private static void HandleActivation(AppActivationArguments args, bool init = false)
    {
        var kind = args.Kind;
        if (kind == ExtendedActivationKind.File)
        {
            _ = HandleFileActivationAsync(args);
        }
        else if (kind == ExtendedActivationKind.Protocol)
        {
            _ = HandleProtocolActivationAsync(args);
        }
        else if (!init)
        {
            var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
            windowManagerProvider.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }
    }

    private static async Task HandleFileActivationAsync(AppActivationArguments args)
    {
        if (args.Data is IFileActivatedEventArgs fileArgs)
        {
            var item = fileArgs.Files.FirstOrDefault();
            if (item is StorageFile file)
            {
                _logger?.LogInformation("App activated via file: {Path}", file.Path);

                var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
                windowManagerProvider.OpenOrShowWindow<SettingsWindow>();

                var pluginManagerControlViewModel = Ioc.Default.GetRequiredService<PluginManagerControlViewModel>();
                await pluginManagerControlViewModel.InstallPluginAsync(file.Path);
            }
        }
    }

    private static async Task HandleProtocolActivationAsync(AppActivationArguments args)
    {
        var windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

        if (args.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            if (protocolArgs.Uri.Host == "link.last.fm")
            {
                var lastFMService = Ioc.Default.GetRequiredService<ILastFmService>();
                await lastFMService.ConfirmAuthAsync(protocolArgs.Uri.Query.Replace("?token=", string.Empty));
                windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
            }
            else if (protocolArgs.Uri.Host == "settings")
            {
                var targetSegment = protocolArgs.Uri.Segments.LastOrDefault()?.Trim('/');
                if (!string.IsNullOrEmpty(targetSegment) &&
                    Enum.TryParse<SettingsSection>(targetSegment, true, out var section))
                {
                    windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
                    var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
                    settingsPageViewModel.NavigateToSection(section);
                }
            }
            else if (protocolArgs.Uri.Host == "lyrics")
            {
                var targetSegment = protocolArgs.Uri.Segments.LastOrDefault()?.Trim('/');
                if (targetSegment == "card")
                {
                    windowManagerProvider.OpenOrShowWindow<LyricsShareWindow>();
                }
                else if (targetSegment == "search")
                {
                    windowManagerProvider.OpenOrShowWindow<LyricsSearchWindow>();

                    var decoder = new WwwFormUrlDecoder(protocolArgs.Uri.Query);
                    var title = decoder.FirstOrDefault(p => p.Name == "title")?.Value ?? "";
                    var artist = decoder.FirstOrDefault(p => p.Name == "artist")?.Value ?? "";
                    var album = decoder.FirstOrDefault(p => p.Name == "album")?.Value ?? "";

                    var lyricsSearchControlViewModel =
                        Ioc.Default.GetRequiredService<LyricsSearchControlViewModel>();
                    lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedTitle = title;
                    lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedArtist = artist;
                    lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedAlbum = album;
                    if (!lyricsSearchControlViewModel.IsSearching)
                        lyricsSearchControlViewModel.SearchCommand.Execute(null);
                }
            }
            else if (protocolArgs.Uri.Host == "player")
            {
                windowManagerProvider.OpenOrShowWindow<MusicGalleryWindow>();
            }
            else if (protocolArgs.Uri.Host == "stats")
            {
                windowManagerProvider.OpenOrShowWindow<StatsDashboardWindow>();
            }
        }
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
                // 数据库服务和迁移
                .AddSingleton<IDatabaseService, DatabaseService>()
                .AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>()

                // 日志
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })

                // Services
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<ISmtcService, SmtcService>()
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
                .AddSingleton<IAddMediaSourceDialogProvider, MediaSourceDialogProvider>()
                .AddSingleton<ILastFmDialogProvider, LastFmDialogProvider>()
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

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEvent(
        IntPtr lpEventAttributes, bool bManualReset,
        bool bInitialState, string lpName);

    [DllImport("kernel32.dll")]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(
        uint dwFlags, uint dwMilliseconds, ulong nHandles,
        IntPtr[] pHandles, out uint dwIndex);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // Do the redirection on another thread, and use a non-blocking
    // wait method to wait for the redirection to complete.
    public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
        _ = Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            SetEvent(redirectEventHandle);
        });

        uint CWMO_DEFAULT = 0;
        var INFINITE = 0xFFFFFFFF;
        _ = CoWaitForMultipleObjects(
            CWMO_DEFAULT, INFINITE, 1,
            [redirectEventHandle], out var handleIndex);

        // Bring the window to the foreground
        var process = Process.GetProcessById((int)keyInstance.ProcessId);
        SetForegroundWindow(process.MainWindowHandle);
    }
}