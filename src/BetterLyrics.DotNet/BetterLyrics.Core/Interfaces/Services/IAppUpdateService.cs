using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IAppUpdateService
{
    AppUpdateStatus AppUpdateStatus { get; }
    string LatestVersion { get; }

    public void StartDailyCheck();

    Task UpdateAvailabilityAsync();
}