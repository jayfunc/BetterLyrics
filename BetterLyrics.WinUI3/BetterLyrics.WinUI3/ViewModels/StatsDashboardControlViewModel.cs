using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Stats;
using BetterLyrics.WinUI3.Services.PlayHistoryService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class StatsDashboardControlViewModel : ObservableObject
    {
        private readonly IPlayHistoryService _playHistoryService;

        public StatsDashboardControlViewModel(IPlayHistoryService playHistoryService)
        {
            _playHistoryService = playHistoryService;
        }

        [ObservableProperty] public partial bool IsLoading { get; set; }

        [ObservableProperty] public partial TimeSpan TotalDuration { get; set; }
        [ObservableProperty] public partial int TotalTracksPlayed { get; set; }
        [ObservableProperty] public partial string TopPlayerName { get; set; } = "N/A";

        public ObservableCollection<SongPlayCount> TopSongs { get; } = new();
        public ObservableCollection<ArtistPlayCount> TopArtists { get; } = new();

        public ObservableCollection<PlayerStatDisplayItem> PlayerStats { get; } = new();

        /// <summary>
        /// 核心方法：根据选中的 Tab 加载数据
        /// </summary>
        [RelayCommand]
        public async Task LoadDataAsync(StatsRange range)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (start, end) = CalculateDateRange(range);

                var durationTask = _playHistoryService.GetTotalListeningDurationAsync(start, end);
                var logsTask = _playHistoryService.GetLogsByDateRangeAsync(start, end);
                var topSongsTask = _playHistoryService.GetTopSongsAsync(start, end, 10);
                var topArtistsTask = _playHistoryService.GetTopArtistsAsync(start, end, 10);
                var playersTask = _playHistoryService.GetPlayerDistributionAsync(start, end);

                await Task.WhenAll(durationTask, logsTask, topSongsTask, topArtistsTask, playersTask);

                TotalDuration = await durationTask;
                var logs = await logsTask;
                TotalTracksPlayed = logs.Count;

                TopSongs.Clear();
                foreach (var item in await topSongsTask) TopSongs.Add(item);

                TopArtists.Clear();
                foreach (var item in await topArtistsTask) TopArtists.Add(item);

                UpdatePlayerStats(await playersTask);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stats: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GenerateTestDataAsync()
        {
            await _playHistoryService.GenerateTestDataAsync(10000);
        }

        /// <summary>
        /// 将原始统计数据转换为带进度条宽度的 UI 数据
        /// </summary>
        private void UpdatePlayerStats(List<PlayerStats> stats)
        {
            PlayerStats.Clear();

            if (stats == null || stats.Count == 0)
            {
                TopPlayerName = "N/A";
                return;
            }

            double maxCount = stats.Max(x => x.Count);
            if (maxCount == 0) maxCount = 1;

            var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
            TopPlayerName = PlayerIdHelper.GetDisplayName(topPlayer?.PlayerId) ?? "N/A";

            foreach (var item in stats.OrderByDescending(x => x.Count))
            {
                double maxBarWidth = 150.0;
                double calculatedWidth = (item.Count / maxCount) * maxBarWidth;

                if (calculatedWidth < 2 && item.Count > 0) calculatedWidth = 2;

                PlayerStats.Add(new PlayerStatDisplayItem
                {
                    PlayerId = item.PlayerId,
                    PlayCount = item.Count,
                    DisplayWidth = calculatedWidth
                });
            }
        }

        private (DateTime Start, DateTime End) CalculateDateRange(StatsRange range)
        {
            DateTime nowLocal = DateTime.Now;
            DateTime startLocal = nowLocal.Date; // 默认为本地今天 00:00

            switch (range)
            {
                case StatsRange.Day:
                    break;
                case StatsRange.Week:
                    int dayOfWeek = (int)nowLocal.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7; // 处理周日
                    startLocal = nowLocal.Date.AddDays(-(dayOfWeek - 1));
                    break;
                case StatsRange.Month:
                    startLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
                    break;
                case StatsRange.Quarter:
                    int quarterStartMonth = (nowLocal.Month - 1) / 3 * 3 + 1;
                    startLocal = new DateTime(nowLocal.Year, quarterStartMonth, 1);
                    break;
                case StatsRange.Year:
                    startLocal = new DateTime(nowLocal.Year, 1, 1);
                    break;
            }

            // 数据库里的 StartedAt 是 UTC，所以查询条件必须也是 UTC
            DateTime startUtc = startLocal.ToUniversalTime();
            DateTime endUtc = nowLocal.ToUniversalTime();

            return (startUtc, endUtc);
        }
    }
}
