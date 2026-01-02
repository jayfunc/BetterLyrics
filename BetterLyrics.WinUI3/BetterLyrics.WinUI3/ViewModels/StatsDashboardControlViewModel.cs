using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Stats;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.PlayHistoryService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;
using Microsoft.UI.Xaml;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class StatsDashboardControlViewModel : ObservableObject
    {
        private readonly IPlayHistoryService _playHistoryService;
        private readonly ILocalizationService _localizationService;
        private readonly IAlbumArtSearchService _albumArtSearchService;

        private string _localizedTimesValue;

        [ObservableProperty] public partial bool IsLoading { get; set; }

        // 时间筛选
        [ObservableProperty] public partial StatsRange SelectedTimeRange { get; set; }
        [ObservableProperty] public partial bool IsCustomRangeSelected { get; set; }
        [ObservableProperty] public partial DateTimeOffset CustomStartDate { get; set; }
        [ObservableProperty] public partial DateTimeOffset CustomEndDate { get; set; } = DateTimeOffset.Now;

        // 顶部基础数据
        [ObservableProperty] public partial TimeSpan TotalDuration { get; set; }
        [ObservableProperty] public partial int TotalTracksPlayed { get; set; }
        [ObservableProperty] public partial string TopPlayerName { get; set; } = "N/A";

        // 时段分布
        [ObservableProperty] public partial ObservableCollection<int> HourlySeriesValues { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<string> HourlyXAxisLabels { get; set; } = [.. Enumerable.Range(0, 24).Select(x => $"{x:D2}:00")];
        [ObservableProperty] public partial string PeakHourText { get; set; } = "--:--";
        [ObservableProperty] public partial string QuietHourText { get; set; } = "--:--";

        // 歌手
        [ObservableProperty] public partial ObservableCollection<ArtistPlayCount> TopArtists { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<int> ArtistSeriesValues { get; set; } = new();
        public Func<ChartPoint, string> ArtistsLabelsFormatter { get; set; }

        // 播放源
        [ObservableProperty] public partial ObservableCollection<ISeries> SourceSeries { get; set; } = new();

        // 歌曲
        [ObservableProperty] public partial ObservableCollection<SongPlayCount> TopSongs { get; set; } = new();

        public StatsDashboardControlViewModel(IPlayHistoryService playHistoryService, ILocalizationService localizationService, IAlbumArtSearchService albumArtSearchService)
        {
            _playHistoryService = playHistoryService;
            _localizationService = localizationService;
            _albumArtSearchService = albumArtSearchService;

            _localizedTimesValue = _localizationService.GetLocalizedString("StatsDashboardControlTimes");

            ArtistsLabelsFormatter = (point) =>
            {
                return TopArtists.ElementAtOrDefault(point.Index)?.Artist ?? "N/A";
            };

            SelectedTimeRange = StatsRange.Today;

            CustomStartDate = DateTimeOffset.Now.AddDays(-7);
            CustomEndDate = DateTimeOffset.Now;
        }

        async partial void OnSelectedTimeRangeChanged(StatsRange value)
        {
            IsCustomRangeSelected = value == StatsRange.Custom;
            if (!IsCustomRangeSelected)
            {
                await LoadDataAsync();
            }
        }
        async partial void OnCustomEndDateChanged(DateTimeOffset value) => await LoadDataAsync();
        async partial void OnCustomStartDateChanged(DateTimeOffset value) => await LoadDataAsync();

        private void ProcessHourlyStats(List<PlayHistoryItem> logs)
        {
            if (logs == null || !logs.Any())
            {
                PeakHourText = "--:--";
                QuietHourText = "--:--";
                HourlySeriesValues = new();
                return;
            }

            var hourCounts = new int[24];
            foreach (var log in logs)
            {
                hourCounts[log.StartedAt.ToLocalTime().Hour]++;
            }

            int peakHour = Array.IndexOf(hourCounts, hourCounts.Max());
            PeakHourText = $"{peakHour:D2}:00 - {peakHour + 1:D2}:00";

            int quietHour = Array.IndexOf(hourCounts, hourCounts.Min());
            QuietHourText = $"{quietHour:D2}:00 - {quietHour + 1:D2}:00";

            HourlySeriesValues = [.. hourCounts];
        }
        private void ProcessArtistStats(List<ArtistPlayCount> artists)
        {
            if (artists == null || !artists.Any())
            {
                ArtistSeriesValues = new();
                return;
            }

            ArtistSeriesValues = [.. artists.Select(x => x.PlayCount)];
        }
        private void UpdatePlayerStats(List<PlayerStats> stats)
        {
            SourceSeries = new();

            if (stats == null || stats.Count == 0)
            {
                TopPlayerName = "N/A";
                return;
            }

            var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
            TopPlayerName = PlayerIdHelper.GetDisplayName(topPlayer?.PlayerId) ?? "N/A";

            var colors = PaletteHelper.GenerateChartColors(ColorHelper.GetSystemAccentColor(), stats.Count);

            SourceSeries = [.. stats.OrderByDescending(x => x.Count).Select((x, i) => new PieSeries<int>
            {
                Values = [x.Count],
                Name = PlayerIdHelper.GetDisplayName(x.PlayerId),
                ToolTipLabelFormatter = point => $"{x.Count} {_localizedTimesValue}",

                Pushout = 4, // 间隙
            })];
        }

        private (DateTime Start, DateTime End) CalculateDateRange()
        {
            // 如果是自定义，直接返回 Picker 的值 (转为 UTC)
            if (IsCustomRangeSelected)
            {
                return (CustomStartDate.UtcDateTime, CustomEndDate.UtcDateTime);
            }

            DateTime nowLocal = DateTime.Now;
            DateTime startLocal = nowLocal.Date;

            switch (SelectedTimeRange)
            {
                case StatsRange.Today:
                    startLocal = nowLocal.Date.AddDays(-1);
                    return (startLocal.ToUniversalTime(), nowLocal.Date.ToUniversalTime());
                case StatsRange.ThisWeek:
                    int dayOfWeek = (int)nowLocal.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7;
                    startLocal = nowLocal.Date.AddDays(-(dayOfWeek - 1));
                    break;
                case StatsRange.ThisMonth:
                    startLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
                    break;
                case StatsRange.ThisQuarter:
                    int quarterStartMonth = (nowLocal.Month - 1) / 3 * 3 + 1;
                    startLocal = new DateTime(nowLocal.Year, quarterStartMonth, 1);
                    break;
                case StatsRange.ThisYear:
                    startLocal = new DateTime(nowLocal.Year, 1, 1);
                    break;
            }

            return (startLocal.ToUniversalTime(), nowLocal.ToUniversalTime());
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (start, end) = CalculateDateRange();

                var durationTask = _playHistoryService.GetTotalListeningDurationAsync(start, end);
                var logsTask = _playHistoryService.GetLogsByDateRangeAsync(start, end);
                var topSongsTask = _playHistoryService.GetTopSongsAsync(start, end, 10);
                var topArtistsTask = _playHistoryService.GetTopArtistsAsync(start, end, 10);
                var playersTask = _playHistoryService.GetPlayerDistributionAsync(start, end);

                await Task.WhenAll(durationTask, logsTask, topSongsTask, topArtistsTask, playersTask);

                TotalDuration = await durationTask;
                var logs = await logsTask;
                TotalTracksPlayed = logs.Count;

                TopSongs = [.. await topSongsTask];

                var pStats = await playersTask;
                UpdatePlayerStats(pStats);

                TopArtists = [.. await topArtistsTask];
                ProcessArtistStats(TopArtists.ToList());

                ProcessHourlyStats(logs);
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
            await _playHistoryService.GenerateTestDataAsync(1000);
            await LoadDataAsync(); // 生成完刷新
        }

    }
}