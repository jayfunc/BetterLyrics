using BetterLyrics.WinUI3.Enums;
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
                // 1. 计算时间范围
                var (start, end) = CalculateDateRange(range);

                // 2. 并行获取所有数据 (性能优化：不等待一个查完再查下一个，而是同时查)
                var durationTask = _playHistoryService.GetTotalListeningDurationAsync(start, end);
                var logsTask = _playHistoryService.GetLogsByDateRangeAsync(start, end); // 用来算总数
                var topSongsTask = _playHistoryService.GetTopSongsAsync(start, end, 10);
                var topArtistsTask = _playHistoryService.GetTopArtistsAsync(start, end, 5); // 只要前5名
                var playersTask = _playHistoryService.GetPlayerDistributionAsync(start, end);

                await Task.WhenAll(durationTask, logsTask, topSongsTask, topArtistsTask, playersTask);

                // 3. 更新 UI 数据
                TotalDuration = await durationTask;
                var logs = await logsTask;
                TotalTracksPlayed = logs.Count;

                // 更新歌曲列表
                TopSongs.Clear();
                foreach (var item in await topSongsTask) TopSongs.Add(item);

                // 更新歌手列表
                TopArtists.Clear();
                foreach (var item in await topArtistsTask) TopArtists.Add(item);

                // 更新播放器分布 (需要特殊处理进度条宽度)
                UpdatePlayerStats(await playersTask);
            }
            catch (Exception ex)
            {
                // 这里可以记录日志 _logger.LogError(ex, ...)
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
                TopPlayerName = "None";
                return;
            }

            double maxCount = stats.Max(x => x.Count);
            if (maxCount == 0) maxCount = 1;

            var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
            TopPlayerName = topPlayer?.PlayerID ?? "None";

            foreach (var item in stats.OrderByDescending(x => x.Count))
            {
                double maxBarWidth = 150.0;
                double calculatedWidth = (item.Count / maxCount) * maxBarWidth;

                if (calculatedWidth < 2 && item.Count > 0) calculatedWidth = 2;

                PlayerStats.Add(new PlayerStatDisplayItem
                {
                    PlayerId = item.PlayerID,
                    PlayCount = item.Count,
                    DisplayWidth = calculatedWidth
                });
            }
        }

        private (DateTime Start, DateTime End) CalculateDateRange(StatsRange range)
        {
            DateTime now = DateTime.Now;
            DateTime start = now;

            switch (range)
            {
                case StatsRange.Day:
                    start = now.Date; // 今天 00:00
                    break;
                case StatsRange.Week:
                    // 假设周一为一周开始
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    start = now.Date.AddDays(-1 * diff);
                    break;
                case StatsRange.Month:
                    start = new DateTime(now.Year, now.Month, 1);
                    break;
                case StatsRange.Quarter:
                    int quarter = (now.Month - 1) / 3 + 1;
                    start = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    break;
                case StatsRange.Year:
                    start = new DateTime(now.Year, 1, 1);
                    break;
            }

            return (start, now);
        }
    }
}
