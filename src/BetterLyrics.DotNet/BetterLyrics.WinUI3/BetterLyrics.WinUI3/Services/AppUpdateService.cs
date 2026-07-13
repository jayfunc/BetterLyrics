using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace BetterLyrics.WinUI3.Services;

public partial class AppUpdateService : BaseViewModel, IAppUpdateService, IDisposable
{
    private readonly IAppUIThreadProvider _appUIThreadProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly StoreContext _storeContext;
    private CancellationTokenSource? _cts;

    public AppUpdateService(ILocalizationService localizationService, ISettingsService settingsService,
        IAppUIThreadProvider appUIThreadProvider)
    {
        _localizationService = localizationService;
        _settingsService = settingsService;
        _appUIThreadProvider = appUIThreadProvider;
        _storeContext = StoreContext.GetDefault();
    }

    [ObservableProperty] public partial AppUpdateStatus AppUpdateStatus { get; set; } = AppUpdateStatus.ErrorOccured;

    [ObservableProperty] public partial string LatestVersion { get; set; } = "-";

    public void StartDailyCheck()
    {
        StopDailyCheck();
        _cts = new CancellationTokenSource();

        _ = Task.Run(() => CheckUpdatePeriodicallyAsync(_cts.Token));
    }

    public async Task UpdateAvailabilityAsync()
    {
        await Task.Delay(Time.WaitingDuration);

        var appUpdateStatus = AppUpdateStatus.ErrorOccured;
        var latestVersion = "-";

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
                .AddButton(new AppNotificationButton(
                        _localizationService.GetLocalizedString("AppUpdateServiceUpdateMS"))
                    .SetInvokeUri(new Uri(Link.StorePage))
                )
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }

        _settingsService.AppSettings.GeneralSettings.LastAppUpateCheckDateTime = DateTime.Now;

        _appUIThreadProvider.Execute(() =>
        {
            AppUpdateStatus = appUpdateStatus;
            LatestVersion = latestVersion;
        });
    }

    public void Dispose()
    {
        StopDailyCheck();
        GC.SuppressFinalize(this);
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
            while (await timer.WaitForNextTickAsync(token)) await UpdateAvailabilityAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }
}