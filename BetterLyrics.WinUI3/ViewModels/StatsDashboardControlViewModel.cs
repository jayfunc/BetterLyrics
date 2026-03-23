using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Entities;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Models.Stats;
using BetterLyrics.WinUI3.Services.AlbumArtSearchService;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.PlayHistoryService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SkiaSharp.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class StatsDashboardControlViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        private readonly IPlayHistoryService _playHistoryService;
        private readonly ILocalizationService _localizationService;
        private readonly IAlbumArtSearchService _albumArtSearchService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<StatsDashboardControlViewModel> _logger;

        private string _localizedTimesValue;

        private readonly DispatcherQueueTimer? _timer;

        [ObservableProperty] public partial IGSMTCService GSMTCService { get; set; }

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

        // 时段分布
        [ObservableProperty] public partial ObservableCollection<int> HourlySeriesValues { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<string> HourlyXAxisLabels { get; set; } = [.. Enumerable.Range(0, 24).Select(x => $"{x:D2}:00")];
        [ObservableProperty] public partial string PeakHourText { get; set; } = "--:--";
        [ObservableProperty] public partial string QuietHourText { get; set; } = "--:--";

        [ObservableProperty] public partial ObservableCollection<ArtistPlayCount> TopArtists { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<ISeries> SourceSeries { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<SongPlayCount> TopSongs { get; set; } = new();

        [ObservableProperty] public partial SolidColorPaint SecondaryTextPaint { get; set; } = new();
        [ObservableProperty] public partial SolidColorPaint PrimaryTextPaint { get; set; } = new();
        [ObservableProperty] public partial SolidColorPaint BackgroundPaint { get; set; } = new();

        public StatsDashboardControlViewModel(
            IPlayHistoryService playHistoryService,
            ILocalizationService localizationService,
            IAlbumArtSearchService albumArtSearchService,
            IGSMTCService gsmtcService,
            ISettingsService settingsService)
        {
            _playHistoryService = playHistoryService;
            _localizationService = localizationService;
            _albumArtSearchService = albumArtSearchService;
            _settingsService = settingsService;
            GSMTCService = gsmtcService;

            _logger = Ioc.Default.GetRequiredService<ILogger<StatsDashboardControlViewModel>>();

            _localizedTimesValue = _localizationService.GetLocalizedString("StatsDashboardControlTimes");

            _timer = DispatcherQueueHelper.Instance?.CreateTimer();

            UpdateDateRange();
            UpdatePaints();
        }

        partial void OnSelectedTimeRangeChanged(StatsRange value)
        {
            IsCustomRangeSelected = value == StatsRange.Custom;
            UpdateDateRange();
        }
        partial void OnCustomEndDateChanged(DateTimeOffset? value) => LoadData();
        partial void OnCustomStartDateChanged(DateTimeOffset? value) => LoadData();
        partial void OnCustomStartTimeChanged(TimeSpan value) => LoadData();
        partial void OnCustomEndTimeChanged(TimeSpan value) => LoadData();

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

        private async Task UpdatePlayerStatsAsync(List<PlayerStats> stats)
        {
            SourceSeries = new();

            if (stats == null || stats.Count == 0)
            {
                TopPlayerName = "N/A";
                return;
            }

            var topPlayer = stats.OrderByDescending(x => x.Count).FirstOrDefault();
            TopPlayerName = await AppHook.GetDisplayNameByAumidAsync(topPlayer?.PlayerId) ?? "N/A";

            var tasks = stats.OrderByDescending(x => x.Count)
                .Select(async (x, i) =>
                {
                    var name = await AppHook.GetDisplayNameByAumidAsync(x.PlayerId) ?? "N/A";
                    return new PieSeries<int>
                    {
                        Values = [x.Count],
                        Name = name,
                        ToolTipLabelFormatter = point => $"{x.Count} {_localizedTimesValue}",
                        Pushout = 4,
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
            DateTime nowLocal = DateTime.Now;
            DateTime startLocal = nowLocal.Date;

            switch (SelectedTimeRange)
            {
                case StatsRange.Today:
                    startLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day);
                    break;
                case StatsRange.ThisWeek:
                    int dayOfWeek = (int)nowLocal.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7;
                    startLocal = nowLocal.Date.AddDays(-(dayOfWeek - 1));
                    startLocal = new DateTime(startLocal.Year, startLocal.Month, startLocal.Day);
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
                await Task.Delay(Constants.Time.WaitingDuration);

                var (start, end) = CalculateDateRange();

                if (start == null || end == null)
                {
                    start = end = DateTime.Now.ToUniversalTime();
                }

                var durationTask = _playHistoryService.GetTotalListeningDurationAsync(start.Value, end.Value);
                var logsTask = _playHistoryService.GetLogsByDateRangeAsync(start.Value, end.Value);
                var topSongsTask = _playHistoryService.GetTopSongsAsync(start.Value, end.Value, 10);
                var topArtistsTask = _playHistoryService.GetTopArtistsAsync(start.Value, end.Value, 10);
                var playersTask = _playHistoryService.GetPlayerDistributionAsync(start.Value, end.Value);

                await Task.WhenAll(durationTask, logsTask, topSongsTask, topArtistsTask, playersTask);

                TotalDuration = await durationTask;
                var logs = await logsTask;
                TotalTracksPlayed = logs.Count;

                TopSongs = [.. await topSongsTask];

                var pStats = await playersTask;
                _ = UpdatePlayerStatsAsync(pStats);

                TopArtists = [.. await topArtistsTask];

                ProcessHourlyStats(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StatsDashboardControlViewModel.LoadDataCoreAsync");
                System.Diagnostics.Debug.WriteLine($"Error loading stats: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdatePaints()
        {
            bool isDark = false;

            switch (_settingsService.AppSettings.GeneralSettings.AppTheme)
            {
                case ElementTheme.Default:
                    isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
                    break;
                case ElementTheme.Dark:
                    isDark = true;
                    break;
                default:
                    break;
            }

            var primaryTextColor = isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 26, 26, 26);
            var secondaryTextColor = isDark ? Color.FromArgb(255, 204, 204, 204) : Color.FromArgb(255, 93, 93, 93);
            var backgroundColor = isDark ? Color.FromArgb(255, 39, 39, 39) : Color.FromArgb(255, 244, 244, 244);
            var shadowColor = isDark ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(40, 0, 0, 0);

            PrimaryTextPaint = primaryTextColor.ToPaint();
            SecondaryTextPaint = secondaryTextColor.ToPaint();
            BackgroundPaint = backgroundColor.ToPaint();
            BackgroundPaint.ImageFilter = new DropShadow(2, 2, 3, 3, shadowColor.ToSKColor());
        }

        [RelayCommand]
        private void RefreshData()
        {
            if (IsCustomRangeSelected)
            {
                LoadData();
            }
            else
            {
                UpdateDateRange();
            }
        }

        [RelayCommand]
        public void LoadData()
        {
            _timer?.Debounce(() =>
            {
                _ = LoadDataCoreAsync();
            }, Constants.Time.DebounceTimeout);
        }

        [RelayCommand]
        private async Task GenerateTestDataAsync()
        {
            await _playHistoryService.GenerateTestDataAsync(1000);
            LoadData(); // 生成完刷新
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.IsScrobbled))
                {
                    if (message.NewValue == true)
                    {
                        RefreshData();
                    }
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                {
                    UpdatePaints();
                }
            }
        }

    }
}