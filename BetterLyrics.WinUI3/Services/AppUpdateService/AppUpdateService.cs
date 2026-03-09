using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace BetterLyrics.WinUI3.Services.AppUpdateService
{
    public partial class AppUpdateService : BaseViewModel, IAppUpdateService, IDisposable
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingsService _settingsService;
        private readonly StoreContext _storeContext;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        public partial AppUpdateStatus AppUpdateStatus { get; set; } = AppUpdateStatus.ErrorOccured;

        [ObservableProperty]
        public partial string LatestVersion { get; set; } = "-";

        [ObservableProperty]
        public partial bool IsChecking { get; set; } = false;

        public AppUpdateService(ILocalizationService localizationService, ISettingsService settingsService)
        {
            _localizationService = localizationService;
            _settingsService = settingsService;
            _storeContext = StoreContext.GetDefault();
        }

        public void StartDailyCheck()
        {
            StopDailyCheck();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => CheckUpdatePeriodicallyAsync(_cts.Token));
        }

        private void StopDailyCheck()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task CheckUpdatePeriodicallyAsync(CancellationToken token)
        {
            await UpdateAvailabilityAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    await UpdateAvailabilityAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async Task UpdateAvailabilityAsync()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsChecking = true;
            });

            await Task.Delay(Constants.Time.WaitingDuration);

            AppUpdateStatus appUpdateStatus = AppUpdateStatus.ErrorOccured;
            string latestVersion = "-";

#if DEBUG
#else
            try
            {
                var packages = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                if (packages != null && packages.Count > 0)
                {
                    appUpdateStatus = AppUpdateStatus.NewAvailable;
                    var version = packages[0].Package.Id.Version;
                    latestVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                }
                else
                {
                    appUpdateStatus = AppUpdateStatus.UpToDate;
                }
            }
            catch (Exception)
            {
            }
#endif

            if (appUpdateStatus == AppUpdateStatus.NewAvailable)
            {
                var notification = new AppNotificationBuilder()
                    .AddText(_localizationService.GetLocalizedString("AppUpdateServiceUpdateAvailable"))
                    .AddText($"{_localizationService.GetLocalizedString("AppUpdateServiceNewVersionAvailable")}")
                    .AddButton(new AppNotificationButton(_localizationService.GetLocalizedString("AppUpdateServiceUpdateMS"))
                        .SetInvokeUri(new Uri(Constants.Link.StorePage))
                    )
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }

            _settingsService.AppSettings.GeneralSettings.LastAppUpateCheckDateTime = DateTime.Now;

            _dispatcherQueue.TryEnqueue(() =>
            {
                AppUpdateStatus = appUpdateStatus;
                LatestVersion = latestVersion;
                IsChecking = false;
            });
        }

        public void Dispose()
        {
            StopDailyCheck();
            GC.SuppressFinalize(this);
        }
    }
}