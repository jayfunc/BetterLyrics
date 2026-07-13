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
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Models.Stats;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace BetterLyrics.Core.ViewModels;

public partial class StatsDashboardControlViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<AppTheme>>
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

    public StatsDashboardControlViewModel(
        IPlayHistoryService playHistoryService,
        ILocalizationService localizationService,
        IAlbumArtSearchService albumArtSearchService,
        IGsmtcService gsmtcService,
        ISettingsService settingsService, ISystemUIProvider systemUiProvider, IProgramProvider programProvider)
    {
        _playHistoryService = playHistoryService;
        _localizationService = localizationService;
        _albumArtSearchService = albumArtSearchService;
        _settingsService = settingsService;
        _systemUiProvider = systemUiProvider;
        _programProvider = programProvider;
        GSMTCService = gsmtcService;

        _logger = Ioc.Default.GetRequiredService<ILogger<StatsDashboardControlViewModel>>();

        _localizedTimesValue = _localizationService.GetLocalizedString("StatsDashboardControlTimes");

        UpdateDateRange();
        UpdatePaints();
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
    [ObservableProperty] public partial ObservableCollection<int> HourlySeriesValues { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> HourlyXAxisLabels { get; set; } =
        [.. Enumerable.Range(0, 24).Select(x => $"{x:D2}:00")];

    [ObservableProperty] public partial string PeakHourText { get; set; } = "--:--";
    [ObservableProperty] public partial string QuietHourText { get; set; } = "--:--";

    [ObservableProperty] public partial ObservableCollection<ArtistPlayCount> TopArtists { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<ISeries> SourceSeries { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<SongPlayCount> TopSongs { get; set; } = new();

    [ObservableProperty] public partial SolidColorPaint SecondaryTextPaint { get; set; } = new();
    [ObservableProperty] public partial SolidColorPaint PrimaryTextPaint { get; set; } = new();
    [ObservableProperty] public partial SolidColorPaint BackgroundPaint { get; set; } = new();

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                UpdatePaints();
    }

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
        CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        if (logs == null || !logs.Any())
        {
            HeatmapData = new ObservableCollection<HeatmapNode>();
            MonthLabels = new ObservableCollection<MonthLabel>();
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

        HeatmapData = new ObservableCollection<HeatmapNode>(nodes);
        MonthLabels = new ObservableCollection<MonthLabel>(monthLabels);
    }

    private void ProcessHourlyStats(List<PlayHistoryItem> logs)
    {
        if (logs == null || !logs.Any())
        {
            PeakHourText = "--:--";
            QuietHourText = "--:--";
            HourlySeriesValues = new ObservableCollection<int>();
            return;
        }

        var hourCounts = new int[24];
        foreach (var log in logs) hourCounts[log.StartedAt.ToLocalTime().Hour]++;

        var peakHour = Array.IndexOf(hourCounts, hourCounts.Max());
        PeakHourText = $"{peakHour:D2}:00 - {peakHour + 1:D2}:00";

        var quietHour = Array.IndexOf(hourCounts, hourCounts.Min());
        QuietHourText = $"{quietHour:D2}:00 - {quietHour + 1:D2}:00";

        HourlySeriesValues = [.. hourCounts];
    }

    private async Task UpdatePlayerStatsAsync(List<PlayerStats> stats)
    {
        SourceSeries = new ObservableCollection<ISeries>();

        if (stats == null || stats.Count == 0)
        {
            TopPlayerName = "N/A";
            return;
        }

        var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
        TopPlayerName = await _programProvider.GetDisplayNameByAumidAsync(topPlayer?.PlayerId) ?? "N/A";

        var tasks = stats.OrderByDescending(x => x.Count)
            .Select(async (x, i) =>
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(x.PlayerId) ?? "N/A";
                return new PieSeries<int>
                {
                    Values = [x.Count],
                    Name = name,
                    ToolTipLabelFormatter = point => $"{x.Count} {_localizedTimesValue}",
                    Pushout = 4
                };
            });

        var resultSeries = await Task.WhenAll(tasks);
        SourceSeries = [.. resultSeries];
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

        CustomStartDate = startLocal.Date;
        CustomEndDate = nowLocal.Date;

        CustomStartTime = startLocal.TimeOfDay;
        CustomEndTime = nowLocal.TimeOfDay;
    }

    private async Task LoadDataCoreAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

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

            TotalDuration = await durationTask;
            var logs = await logsTask;
            TotalTracksPlayed = logs.Count;

            TopSongs = [.. await topSongsTask];

            var pStats = await playersTask;
            _ = UpdatePlayerStatsAsync(pStats);

            TopArtists = [.. await topArtistsTask];

            ProcessHeatmapStats(logs, start.Value, end.Value);
            ProcessHourlyStats(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StatsDashboardControlViewModel.LoadDataCoreAsync");
            Debug.WriteLine($"Error loading stats: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdatePaints()
    {
        var isDark = false;

        switch (_settingsService.AppSettings.GeneralSettings.AppTheme)
        {
            case AppTheme.Default:
                isDark = _systemUiProvider.GetAppTheme() == AppTheme.Dark;
                break;
            case AppTheme.Dark:
                isDark = true;
                break;
        }

        var primaryTextColor = isDark ? new SKColor(255, 255, 255, 255) : new SKColor(26, 26, 26, 255);
        var secondaryTextColor = isDark ? new SKColor(204, 204, 204, 255) : new SKColor(93, 93, 93, 255);
        var backgroundColor = isDark ? new SKColor(39, 39, 39, 255) : new SKColor(244, 244, 244, 255);
        var shadowColor = isDark ? new SKColor(0, 0, 0, 150) : new SKColor(0, 0, 0, 40);

        PrimaryTextPaint = new SolidColorPaint(primaryTextColor);
        SecondaryTextPaint = new SolidColorPaint(secondaryTextColor);
        BackgroundPaint = new SolidColorPaint(backgroundColor);
        BackgroundPaint.ImageFilter = new DropShadow(2, 2, 3, 3, shadowColor);
    }

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
        LoadData(); // 生成完刷新
    }
}