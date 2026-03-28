// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.DbContext;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.AppLifecycleService;
using BetterLyrics.WinUI3.Services.AppUpdateService;
using BetterLyrics.WinUI3.Services.DiscordService;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.FileWatchService;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.LyricsCacheService;
using BetterLyrics.WinUI3.Services.LyricsSearchService;
using BetterLyrics.WinUI3.Services.NavigationService;
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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
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

namespace BetterLyrics.WinUI3
{
    public class Program
    {
        private static ILogger<Program>? _logger;

        [STAThread]
        static int Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            bool isRedirect = false;
            
            try
            {
                isRedirect = DecideRedirection();
            }
            catch (Exception)
            {
                isRedirect = false;
            }

            if (!isRedirect)
            {
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);

                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    PathHelper.EnsureDirectories();
                    ConfigureServices();
                    _logger = Ioc.Default.GetRequiredService<ILogger<Program>>();

                    _ = new App();

                    try
                    {
                        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                        HandleActivation(activatedArgs, true);
                    }
                    catch (Exception)
                    {
                        HandleActivation(null, true);
                    }
                });
            }

            return 0;
        }

        private static bool DecideRedirection()
        {
            bool isRedirect = false;
            try
            {
                AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
                ExtendedActivationKind kind = args.Kind;
                AppInstance keyInstance = AppInstance.FindOrRegisterForKey("MySingleInstanceApp");

                if (keyInstance.IsCurrent)
                {
                    keyInstance.Activated += OnActivated;
                }
                else
                {
                    isRedirect = true;
                    RedirectActivationTo(args, keyInstance);
                }
            }
            catch (Exception)
            {
                isRedirect = false;
            }

            return isRedirect;
        }

        private static void OnActivated(object? sender, AppActivationArguments args)
        {
            DispatcherQueueHelper.Instance?.TryEnqueue(() =>
            {
                HandleActivation(args);
            });
        }

        private static void HandleActivation(AppActivationArguments? args, bool init = false)
        {
            if (args != null)
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
                    WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
                }
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

                    WindowHook.OpenOrShowWindow<SettingsWindow>();

                    var pluginManagerControlViewModel = Ioc.Default.GetRequiredService<PluginManagerControlViewModel>();
                    await pluginManagerControlViewModel.InstallPluginByFileAsync(file);
                }
            }
        }

        private static async Task HandleProtocolActivationAsync(AppActivationArguments args)
        {
            if (args.Data is IProtocolActivatedEventArgs protocolArgs)
            {
                if (protocolArgs.Uri.Host == "link.last.fm")
                {
                    var lastFMService = Ioc.Default.GetRequiredService<ILastFMService>();
                    await lastFMService.ConfirmAuthAsync(protocolArgs.Uri.Query.Replace("?token=", string.Empty));
                    WindowHook.OpenOrShowWindow<SettingsWindow>();
                }
                else if (protocolArgs.Uri.Host == "settings")
                {
                    var targetSegment = protocolArgs.Uri.Segments.LastOrDefault()?.Trim('/');
                    if (!string.IsNullOrEmpty(targetSegment) && Enum.TryParse<SettingsSection>(targetSegment, true, out var section))
                    {
                        _ = WindowHook.OpenOrShowWindow<SettingsWindow>();
                        var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
                        settingsPageViewModel.NavigateToSection(section);
                    }
                }
                else if (protocolArgs.Uri.Host == "lyrics")
                {
                    var targetSegment = protocolArgs.Uri.Segments.LastOrDefault()?.Trim('/');
                    if (targetSegment == "card")
                    {
                        WindowHook.OpenOrShowWindow<LyricsShareWindow>();
                    }
                    else if (targetSegment == "search")
                    {
                        WindowHook.OpenOrShowWindow<LyricsSearchWindow>();

                        var decoder = new WwwFormUrlDecoder(protocolArgs.Uri.Query);
                        var title = decoder.FirstOrDefault(p => p.Name == "title")?.Value ?? "";
                        var artist = decoder.FirstOrDefault(p => p.Name == "artist")?.Value ?? "";
                        var album = decoder.FirstOrDefault(p => p.Name == "album")?.Value ?? "";

                        var lyricsSearchControlViewModel = Ioc.Default.GetRequiredService<LyricsSearchControlViewModel>();
                        lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedTitle = title;
                        lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedArtist = artist;
                        lyricsSearchControlViewModel.MappedSongSearchQuery?.MappedAlbum = album;
                        if (!lyricsSearchControlViewModel.IsSearching)
                        {
                            lyricsSearchControlViewModel.SearchCommand.Execute(null);
                        }
                    }
                }
                else if (protocolArgs.Uri.Host == "player")
                {
                    WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
                }
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
                    .AddDbContextFactory<PlayHistoryDbContext>(options => options.UseSqlite($"Data Source={PathHelper.PlayHistoryPath}"))
                    .AddDbContextFactory<FilesIndexDbContext>(options => options.UseSqlite($"Data Source={PathHelper.FilesIndexPath}"))
                    .AddDbContextFactory<LyricsCacheDbContext>(options => options.UseSqlite($"Data Source={PathHelper.LyricsCachePath}"))
                    .AddDbContextFactory<SongSearchMapDbContext>(options => options.UseSqlite($"Data Source={PathHelper.SongSearchMapPath}"))

                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddSerilog();
                    })

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
                    .AddSingleton<IFileWatchService, FileWatchService>()
                    .AddSingleton<IAppUpdateService, AppUpdateService>()
                    .AddSingleton<INavigationService, NavigationService>()
                    .AddSingleton<IAppLifecycleService, AppLifecycleService>()

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
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
        {
            redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
            _ = Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                SetEvent(redirectEventHandle);
            });

            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = CoWaitForMultipleObjects(
               CWMO_DEFAULT, INFINITE, 1,
               [redirectEventHandle], out uint handleIndex);

            Process process = Process.GetProcessById((int)keyInstance.ProcessId);
            SetForegroundWindow(process.MainWindowHandle);
        }

    }
}
