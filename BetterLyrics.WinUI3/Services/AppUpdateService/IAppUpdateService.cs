using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.AppUpdateService
{
    public interface IAppUpdateService
    {
        AppUpdateStatus AppUpdateStatus { get; }
        string LatestVersion { get; }

        public void StartDailyCheck();

        Task UpdateAvailabilityAsync();
    }
}
