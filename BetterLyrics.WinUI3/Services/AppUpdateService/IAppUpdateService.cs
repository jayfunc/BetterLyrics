using BetterLyrics.WinUI3.Enums;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.AppUpdateService
{
    public interface IAppUpdateService
    {
        AppUpdateStatus AppUpdateStatus { get; }
        string LatestVersion { get; }
        bool IsChecking { get; }

        public void StartDailyCheck();

        Task UpdateAvailabilityAsync();
    }
}
