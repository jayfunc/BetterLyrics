using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Stats;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.ViewModels;

public partial class StatsDashboardControlViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<bool>>
{
    private readonly IAlbumArtSearchService _albumArtSearchService;

    private readonly Debouncer _debouncer = new();
    private readonly ILocalizationService _localizationService;

    private readonly string _localizedTimesValue;
    private readonly ILogger<StatsDashboardControlViewModel> _logger;
    private readonly IPlayHistoryService _playHistoryService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemUIProvider _systemUiProvider;
    private readonly IProgramProvider _programProvider;
    private readonly IAppUIThreadProvider _appUIThreadProvider;

    public StatsDashboardControlViewModel(
        IPlayHistoryService playHistoryService,
        ILocalizationService localizationService,
        IAlbumArtSearchService albumArtSearchService,
        IGsmtcService gsmtcService,
        ISettingsService settingsService, ISystemUIProvider systemUiProvider, IProgramProvider programProvider,
        IAppUIThreadProvider appUIThreadProvider)
    {
        _playHistoryService = playHistoryService;
        _localizationService = localizationService;
        _albumArtSearchService = albumArtSearchService;
        _settingsService = settingsService;
        _systemUiProvider = systemUiProvider;
        _programProvider = programProvider;
        _appUIThreadProvider = appUIThreadProvider;
        GSMTCService = gsmtcService;

        _logger = Ioc.Default.GetRequiredService<ILogger<StatsDashboardControlViewModel>>();

        _localizedTimesValue = _localizationService.GetLocalizedString("StatsDashboardControlTimes");

        UpdateDateRange();
    }

    [ObservableProperty] public partial IGsmtcService GSMTCService { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; } = false;

    // 时间筛选
    [ObservableProperty] public partial StatsRange SelectedTimeRange { get; set; } = StatsRange.Today;
    [ObservableProperty] public partial bool IsCustomRangeSelected { get; set; } = false;
    [ObservableProperty] public partial DateTimeOffset? CustomStartDate { get; set; } = DateTime.Now;
    [ObservableProperty] public partial DateTimeOffset? CustomEndDate { get; set; } = DateTime.Now;
    [ObservableProperty] public partial TimeSpan CustomStartTime { get; set; } = TimeSpan.Zero;
    [ObservableProperty] public partial TimeSpan CustomEndTime { get; set; } = TimeSpan.Zero;

    // 顶部基础数据
    [ObservableProperty] public partial TimeSpan TotalDuration { get; set; }
    [ObservableProperty] public partial int TotalTracksPlayed { get; set; }
    [ObservableProperty] public partial string TopPlayerName { get; set; } = "N/A";

    // GitHub 热度图
    [ObservableProperty] public partial ObservableCollection<HeatmapNode> HeatmapData { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<MonthLabel> MonthLabels { get; set; } = new();

    // 时段分布
    [ObservableProperty] public partial ObservableCollection<HourlyActivityItem> HourlySeriesValues { get; set; } = new();

    [ObservableProperty] public partial string PeakHourText { get; set; } = "--:--";
    [ObservableProperty] public partial string QuietHourText { get; set; } = "--:--";

    [ObservableProperty] public partial ObservableCollection<ArtistPlayCount> TopArtists { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<PlayerSourceItem> SourceSeries { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<SongPlayCount> TopSongs { get; set; } = new();

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.IsScrobbled))
                if (message.NewValue)
                    RefreshData();
    }

    partial void OnSelectedTimeRangeChanged(StatsRange value)
    {
        IsCustomRangeSelected = value == StatsRange.Custom;
        UpdateDateRange();
    }

    partial void OnCustomEndDateChanged(DateTimeOffset? value)
    {
        LoadData();
    }

    partial void OnCustomStartDateChanged(DateTimeOffset? value)
    {
        LoadData();
    }

    partial void OnCustomStartTimeChanged(TimeSpan value)
    {
        LoadData();
    }

    partial void OnCustomEndTimeChanged(TimeSpan value)
    {
        LoadData();
    }

    private void ProcessHeatmapStats(List<PlayHistoryItem> logs, DateTime start, DateTime end,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        if (logs == null || !logs.Any())
        {
            _appUIThreadProvider.Execute(() =>
            {
                HeatmapData = new ObservableCollection<HeatmapNode>();
                MonthLabels = new ObservableCollection<MonthLabel>();
            });
            return;
        }

        var startDate = start.Date;
        var endDate = end.Date;

        var dailyCounts = logs
            .GroupBy(x => x.StartedAt.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var maxCount = dailyCounts.Values.DefaultIfEmpty(0).Max();
        var nodes = new List<HeatmapNode>();
        var monthLabels = new List<MonthLabel>();

        var startDayOfWeek = (int)culture.DateTimeFormat.FirstDayOfWeek;
        for (var i = 0; i < startDayOfWeek; i++) nodes.Add(new HeatmapNode { IsEmpty = true });

        var currentMonth = startDate.Month;
        var currentYear = startDate.Year;

        if (DateTime.DaysInMonth(startDate.Year, startDate.Month) - startDate.Day >= 15)
            monthLabels.Add(new MonthLabel
            {
                Name = startDate.ToString("MMM", culture),
                Offset = 0
            });

        var days = (int)(endDate - startDate).TotalDays + 1;

        for (var i = 0; i < days; i++)
        {
            var currentDate = startDate.AddDays(i);

            if (currentDate.Month != currentMonth)
            {
                currentMonth = currentDate.Month;

                var colIndex = nodes.Count / 7;
                double offset = colIndex * 18 + 2;

                string labelName;

                if (currentDate.Year != currentYear)
                {
                    currentYear = currentDate.Year;
                    labelName = currentDate.ToString("y", culture);
                }
                else
                {
                    labelName = currentDate.ToString("MMM", culture);
                }

                monthLabels.Add(new MonthLabel
                {
                    Name = labelName,
                    Offset = offset
                });
            }

            var count = dailyCounts.TryGetValue(currentDate, out var c) ? c : 0;
            var level = 0;
            if (count > 0)
            {
                if (maxCount <= 4)
                {
                    level = count;
                }
                else
                {
                    var ratio = (double)count / maxCount;
                    if (ratio <= 0.25) level = 1;
                    else if (ratio <= 0.5) level = 2;
                    else if (ratio <= 0.75) level = 3;
                    else level = 4;
                }
            }

            nodes.Add(new HeatmapNode
            {
                Date = currentDate,
                PlayCount = count,
                Level = level,
                IsEmpty = false
            });
        }

        _appUIThreadProvider.Execute(() =>
        {
            HeatmapData = new ObservableCollection<HeatmapNode>(nodes);
            MonthLabels = new ObservableCollection<MonthLabel>(monthLabels);
        });
    }

    private void ProcessHourlyStats(List<PlayHistoryItem> logs)
    {
        if (logs == null || !logs.Any())
        {
            _appUIThreadProvider.Execute(() =>
            {
                PeakHourText = "--:--";
                QuietHourText = "--:--";
                HourlySeriesValues = new ObservableCollection<HourlyActivityItem>();
            });
            return;
        }

        var hourCounts = new int[24];
        foreach (var log in logs) hourCounts[log.StartedAt.ToLocalTime().Hour]++;

        var maxHourCount = hourCounts.Max();
        var peakHour = Array.IndexOf(hourCounts, maxHourCount);
        var peakHourStr = $"{peakHour:D2}:00 - {peakHour + 1:D2}:00";

        var quietHour = Array.IndexOf(hourCounts, hourCounts.Min());
        var quietHourStr = $"{quietHour:D2}:00 - {quietHour + 1:D2}:00";

        var items = new List<HourlyActivityItem>();
        for (int i = 0; i < 24; i++)
        {
            items.Add(new HourlyActivityItem
            {
                TimeLabel = $"{i:D2}:00",
                Count = hourCounts[i],
                HeightPercentage = maxHourCount == 0 ? 0 : (double)hourCounts[i] / maxHourCount,
                TooltipText = $"{hourCounts[i]} {_localizedTimesValue}"
            });
        }

        _appUIThreadProvider.Execute(() =>
        {
            PeakHourText = peakHourStr;
            QuietHourText = quietHourStr;
            HourlySeriesValues = [.. items];
        });
    }

    private async Task UpdatePlayerStatsAsync(List<PlayerStats> stats)
    {
        if (stats == null || stats.Count == 0)
        {
            _appUIThreadProvider.Execute(() =>
            {
                SourceSeries = new ObservableCollection<PlayerSourceItem>();
                TopPlayerName = "N/A";
            });
            return;
        }

        var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
        var topPlayerName = await _programProvider.GetDisplayNameByAumidAsync(topPlayer?.PlayerId) ?? "N/A";

        double totalCount = stats.Sum(x => x.Count);

        var tasks = stats.OrderByDescending(x => x.Count)
            .Select(async x =>
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(x.PlayerId) ?? "N/A";
                return new PlayerSourceItem
                {
                    Name = name,
                    Count = x.Count,
                    Percentage = totalCount == 0 ? 0 : (double)x.Count / totalCount
                };
            });

        var resultSeries = await Task.WhenAll(tasks);
        
        _appUIThreadProvider.Execute(() =>
        {
            SourceSeries = [.. resultSeries];
            TopPlayerName = topPlayerName;
        });
    }

    private (DateTime? Start, DateTime? End) CalculateDateRange()
    {
        if (CustomStartDate == null || CustomEndDate == null) return (null, null);

        return (
            new DateTime(
                    DateOnly.FromDateTime(CustomStartDate.Value.LocalDateTime),
                    TimeOnly.FromTimeSpan(CustomStartTime),
                    DateTimeKind.Local)
                .ToUniversalTime(),
            new DateTime(
                    DateOnly.FromDateTime(CustomEndDate.Value.LocalDateTime),
                    TimeOnly.FromTimeSpan(CustomEndTime),
                    DateTimeKind.Local)
                .ToUniversalTime()
        );
    }

    private void UpdateDateRange()
    {
        var nowLocal = DateTime.Now;
        var startLocal = nowLocal.Date;

        switch (SelectedTimeRange)
        {
            case StatsRange.Today:
                startLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day);
                break;
            case StatsRange.ThisWeek:
                var dayOfWeek = (int)nowLocal.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;
                startLocal = nowLocal.Date.AddDays(-(dayOfWeek - 1));
                startLocal = new DateTime(startLocal.Year, startLocal.Month, startLocal.Day);
                break;
            case StatsRange.ThisMonth:
                startLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
                break;
            case StatsRange.ThisQuarter:
                var quarterStartMonth = (nowLocal.Month - 1) / 3 * 3 + 1;
                startLocal = new DateTime(nowLocal.Year, quarterStartMonth, 1);
                break;
            case StatsRange.ThisYear:
                startLocal = new DateTime(nowLocal.Year, 1, 1);
                break;
            case StatsRange.AllTime:
                startLocal = new DateTime(2025, 5, 13, 2, 53, 2, DateTimeKind.Utc).ToLocalTime();
                break;
        }

        _appUIThreadProvider.Execute(() =>
        {
            CustomStartDate = startLocal.Date;
            CustomEndDate = nowLocal.Date;
            CustomStartTime = startLocal.TimeOfDay;
            CustomEndTime = nowLocal.TimeOfDay;
        });
    }

    private async Task LoadDataCoreAsync()
    {
        if (IsLoading) return;
        _appUIThreadProvider.Execute(() => IsLoading = true);

        try
        {
            await Task.Delay(Time.WaitingDuration);

            var (start, end) = CalculateDateRange();

            if (start == null || end == null) start = end = DateTime.Now.ToUniversalTime();

            var durationTask = _playHistoryService.GetTotalListeningDurationAsync(start.Value, end.Value);
            var logsTask = _playHistoryService.GetLogsByDateRangeAsync(start.Value, end.Value);
            var topSongsTask = _playHistoryService.GetTopSongsAsync(start.Value, end.Value);
            var topArtistsTask = _playHistoryService.GetTopArtistsAsync(start.Value, end.Value);
            var playersTask = _playHistoryService.GetPlayerDistributionAsync(start.Value, end.Value);

            await Task.WhenAll(durationTask, logsTask, topSongsTask, topArtistsTask, playersTask);

            var duration = await durationTask;
            var logs = await logsTask;
            var topSongs = await topSongsTask;
            var topArtists = await topArtistsTask;
            var pStats = await playersTask;

            await UpdatePlayerStatsAsync(pStats);

            _appUIThreadProvider.Execute(() =>
            {
                TotalDuration = duration;
                TotalTracksPlayed = logs.Count;
                TopSongs = [.. topSongs];
                TopArtists = [.. topArtists];
            });

            ProcessHeatmapStats(logs, start.Value, end.Value);
            ProcessHourlyStats(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StatsDashboardControlViewModel: LoadDataCoreAsync");
            Debug.WriteLine($"Error loading stats: {ex.Message}");
        }
        finally
        {
            _appUIThreadProvider.Execute(() => IsLoading = false);
        }
    }

    // UpdatePaints removed

    [RelayCommand]
    private void RefreshData()
    {
        if (IsCustomRangeSelected)
            LoadData();
        else
            UpdateDateRange();
    }

    [RelayCommand]
    public void LoadData()
    {
        _ = _debouncer.RunAsync(() => { _ = LoadDataCoreAsync(); });
    }

    [RelayCommand]
    private async Task GenerateTestDataAsync()
    {
        await _playHistoryService.GenerateTestDataAsync(1000);
        LoadData(); // Refresh data after generating test data
    }
}