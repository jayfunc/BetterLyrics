using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class NowPlayingBarViewModel : ObservableRecipient, ITransientViewModel,
    IRecipient<CurrentPositionChangedMessage>,
    IRecipient<CurrentMediaSourceProviderChangedMessage>
{
    private readonly IGsmtcService _gsmtcService;
    private readonly ISettingsService _settingsService;
    private readonly IAudioMixerService _audioMixerService;
    private readonly ISmtcService _smtcService;
    private readonly IAppUIThreadProvider _appUIThreadProvider;

    [ObservableProperty] public partial double SimulatedPositionSeconds { get; set; } = 0.0;

    public INavigationService NavigationService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial int Volume { get; set; } = -1;
    [ObservableProperty] public partial int SystemVolume { get; set; } = -1;

    [ObservableProperty] public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

    [ObservableProperty] public partial double TimelineSliderThumbSeconds { get; set; } = 0;

    /// <summary>
    /// 为 true 时仅反映并控制内置播放器，忽略其他系统媒体会话。
    /// </summary>
    [ObservableProperty] public partial bool IsBuiltInOnly { get; set; }

    /// <summary>
    /// 与权威位置的偏差达到该值时判定为真正的跳转（拖动、切歌、恢复播放），立即对齐。
    /// 低于该值的偏差只调整累加速率，让进度条连续移动而不是瞬时回跳。
    /// </summary>
    private const double SeekSnapThresholdSeconds = 3.0;

    /// <summary>
    /// 死区内的偏差不做速率修正，避免播放器每次上报时间轴都改变进度条的移动速度。
    /// </summary>
    private const double DriftDeadbandSeconds = 0.12;

    /// <summary>
    /// 落后时最多加速到 1.5 倍，保证追赶过程本身也是连续移动。
    /// </summary>
    private const double MaxCatchUpRate = 1.5;

    private byte[]? _builtInAlbumArtBytes;
    private SongInfo? _hookedSongInfo;
    private double _latestRealPositionSeconds;
    private long _ignorePositionChangeUntilTicks;

    public string Title => IsBuiltInOnly
        ? _smtcService.PlayingTrack is { } track ? (string.IsNullOrEmpty(track.Title) ? track.FileName : track.Title) : ""
        : _gsmtcService.CurrentSongInfo.Title;

    public string Artist => IsBuiltInOnly
        ? _smtcService.PlayingTrack?.Artist ?? ""
        : _gsmtcService.CurrentSongInfo.Artist;

    public byte[]? AlbumArtBytes => IsBuiltInOnly ? _builtInAlbumArtBytes : _gsmtcService.AlbumArtBytes;

    public bool IsPlaying => IsBuiltInOnly ? _smtcService.IsPlaying : _gsmtcService.CurrentIsPlaying;

    public double DurationSeconds => IsBuiltInOnly
        ? _smtcService.PlayingTrack?.Duration ?? 0
        : _gsmtcService.CurrentSongInfo.DurationMs / 1000.0;

    public bool IsExternalSourceInfoVisible => !IsBuiltInOnly;

    public NowPlayingBarViewModel(IGsmtcService mediaSessionsService, ISettingsService settingsService,
    INavigationService navigationService, IAudioMixerService audioMixerService,
    ISmtcService smtcService, IAppUIThreadProvider appUIThreadProvider)
    {
        _gsmtcService = mediaSessionsService;
        _settingsService = settingsService;
        _audioMixerService = audioMixerService;
        _smtcService = smtcService;
        _appUIThreadProvider = appUIThreadProvider;

        NavigationService = navigationService;
        AppSettings = _settingsService.AppSettings;

        UpdateVolume();
    }

    public void UpdateVolume()
    {
        var provider = IsBuiltInOnly ? null : _gsmtcService.CurrentMediaSourceProviderInfo?.Provider;
        var isBetterLyrics = IsBuiltInOnly || _gsmtcService.CurrentMediaSourceProviderInfo?.IsBetterLyrics == true ||
            (_gsmtcService.CurrentMediaSourceProviderInfo == null && _smtcService.PlayingTrack != null);

        var appVolume = IsBuiltInOnly
            ? _audioMixerService.GetApplicationVolume(Environment.ProcessId)
            : _audioMixerService.GetApplicationVolume(provider);

        if (isBetterLyrics)
        {
            var savedVolume = _settingsService.AppSettings.MusicGallerySettings.Volume;
            if (appVolume == -1)
            {
                appVolume = savedVolume;
            }
            else if (!_smtcService.IsVolumeRestored && appVolume != savedVolume)
            {
                _audioMixerService.SetApplicationVolume(Environment.ProcessId, savedVolume);
                _smtcService.IsVolumeRestored = true;
                appVolume = savedVolume;
            }
        }

        Volume = appVolume;
        SystemVolume = _audioMixerService.GetSystemVolume();
    }

    public void ApplyVolume()
    {
        var provider = IsBuiltInOnly ? null : _gsmtcService.CurrentMediaSourceProviderInfo?.Provider;
        var isBetterLyrics = IsBuiltInOnly || _gsmtcService.CurrentMediaSourceProviderInfo?.IsBetterLyrics == true ||
            (_gsmtcService.CurrentMediaSourceProviderInfo == null && _smtcService.PlayingTrack != null);

        if (isBetterLyrics)
        {
            _audioMixerService.SetApplicationVolume(Environment.ProcessId, Volume);
            _settingsService.AppSettings.MusicGallerySettings.Volume = Volume;
            _smtcService.IsVolumeRestored = true;
        }
        else
        {
            _audioMixerService.SetApplicationVolume(provider, Volume);
        }
    }

    public void ApplySystemVolume()
    {
        _audioMixerService.SetSystemVolume(SystemVolume);
    }

    public void SyncBuiltInPosition()
    {
        SimulatedPositionSeconds = _smtcService.CurrentPosition.TotalSeconds;
    }

    /// <summary>
    /// 由 UI 定时器按真实流逝时间推进进度条。
    /// </summary>
    public void AdvanceSimulatedPosition(double elapsedSeconds)
    {
        if (elapsedSeconds <= 0) return;

        var drift = _latestRealPositionSeconds - SimulatedPositionSeconds;

        var rate = drift > DriftDeadbandSeconds
            ? Math.Clamp(1.0 + drift * 0.5, 1.0, MaxCatchUpRate)
            : 1.0;

        SetSimulatedPosition(SimulatedPositionSeconds + elapsedSeconds * rate);
    }

    private void SetSimulatedPosition(double seconds)
    {
        var duration = DurationSeconds;
        if (duration > 0 && seconds > duration) seconds = duration;
        if (seconds < 0) seconds = 0;

        SimulatedPositionSeconds = seconds;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _gsmtcService.PropertyChanged += GsmtcService_PropertyChanged;
        _smtcService.PropertyChanged += SmtcService_PropertyChanged;
        HookSongInfo();
        RefreshBuiltInAlbumArt();

        // 窗口中途打开时直接落到当前进度，避免从 0 跳到权威位置
        if (!IsBuiltInOnly)
        {
            _latestRealPositionSeconds = _gsmtcService.CurrentPosition.TotalSeconds;
            SimulatedPositionSeconds = _latestRealPositionSeconds;
        }
    }

    protected override void OnDeactivated()
    {
        _gsmtcService.PropertyChanged -= GsmtcService_PropertyChanged;
        _smtcService.PropertyChanged -= SmtcService_PropertyChanged;
        UnhookSongInfo();
        base.OnDeactivated();
    }

    partial void OnIsBuiltInOnlyChanged(bool value)
    {
        RefreshBuiltInAlbumArt();
        RaiseSourceStateChanged();
        UpdateVolume();

        // 切换播放源后两边的进度基准不同，用最新的权威位置重置，避免被判定为跳转而弹跳
        _latestRealPositionSeconds = value
            ? _smtcService.CurrentPosition.TotalSeconds
            : _gsmtcService.CurrentPosition.TotalSeconds;

        SetSimulatedPosition(_latestRealPositionSeconds);
    }

    private void RaiseSourceStateChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Artist));
        OnPropertyChanged(nameof(AlbumArtBytes));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(DurationSeconds));
        OnPropertyChanged(nameof(IsExternalSourceInfoVisible));
    }

    private void HookSongInfo()
    {
        UnhookSongInfo();
        _hookedSongInfo = _gsmtcService.CurrentSongInfo;
        _hookedSongInfo.PropertyChanged += SongInfo_PropertyChanged;
    }

    private void UnhookSongInfo()
    {
        if (_hookedSongInfo != null)
        {
            _hookedSongInfo.PropertyChanged -= SongInfo_PropertyChanged;
            _hookedSongInfo = null;
        }
    }

    private void SongInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _appUIThreadProvider.Execute(() =>
        {
            if (IsBuiltInOnly) return;
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(DurationSeconds));
        });
    }

    private void GsmtcService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _appUIThreadProvider.Execute(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(IGsmtcService.CurrentSongInfo):
                    HookSongInfo();
                    if (!IsBuiltInOnly)
                    {
                        OnPropertyChanged(nameof(Title));
                        OnPropertyChanged(nameof(Artist));
                        OnPropertyChanged(nameof(DurationSeconds));
                    }
                    break;
                case nameof(IGsmtcService.AlbumArtBytes):
                    if (!IsBuiltInOnly) OnPropertyChanged(nameof(AlbumArtBytes));
                    break;
                case nameof(IGsmtcService.CurrentIsPlaying):
                    if (!IsBuiltInOnly) OnPropertyChanged(nameof(IsPlaying));
                    break;
            }
        });
    }

    private void SmtcService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _appUIThreadProvider.Execute(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ISmtcService.PlayingTrack):
                    RefreshBuiltInAlbumArt();
                    if (IsBuiltInOnly)
                    {
                        OnPropertyChanged(nameof(Title));
                        OnPropertyChanged(nameof(Artist));
                        OnPropertyChanged(nameof(DurationSeconds));
                    }
                    break;
                case nameof(ISmtcService.IsPlaying):
                    if (IsBuiltInOnly) OnPropertyChanged(nameof(IsPlaying));
                    break;
            }
        });
    }

    private void RefreshBuiltInAlbumArt()
    {
        if (!IsBuiltInOnly) return;
        _ = RefreshBuiltInAlbumArtAsync();
    }

    private async Task RefreshBuiltInAlbumArtAsync()
    {
        var track = _smtcService.PlayingTrack;
        var bytes = track?.AlbumArtByteArray;

        if (bytes == null && !string.IsNullOrEmpty(track?.LocalAlbumArtPath))
        {
            try
            {
                if (File.Exists(track.LocalAlbumArtPath))
                {
                    bytes = await File.ReadAllBytesAsync(track.LocalAlbumArtPath);
                }
            }
            catch
            {
                bytes = null;
            }
        }

        if (!ReferenceEquals(track, _smtcService.PlayingTrack)) return;

        _builtInAlbumArtBytes = bytes;
        if (IsBuiltInOnly) OnPropertyChanged(nameof(AlbumArtBytes));
    }

    partial void OnTimelineSliderThumbSecondsChanged(double value)
    {
        TimelineSliderThumbLyricsLine = _gsmtcService.CurrentLyricsData?.GetLyricsLine(value);
    }


    [RelayCommand]
    private async Task PlaySongAsync()
    {
        if (IsBuiltInOnly)
        {
            if (_smtcService.PlayingTrack == null && _smtcService.TrackPlayingQueue.Count > 0)
            {
                _smtcService.PlayTrackAt(0);
            }
            else
            {
                _smtcService.Play();
            }
            return;
        }
        await _gsmtcService.PlayAsync();
    }

    [RelayCommand]
    private async Task PauseSongAsync()
    {
        if (IsBuiltInOnly)
        {
            _smtcService.Pause();
            return;
        }
        await _gsmtcService.PauseAsync();
    }

    [RelayCommand]
    private async Task PreviousSongAsync()
    {
        if (IsBuiltInOnly)
        {
            _smtcService.PlayPreviousTrack();
            return;
        }
        await _gsmtcService.PreviousAsync();
    }

    [RelayCommand]
    private async Task NextSongAsync()
    {
        if (IsBuiltInOnly)
        {
            _smtcService.PlayNextTrack();
            return;
        }
        await _gsmtcService.NextAsync();
    }

    [RelayCommand]
    private async Task StopTrackAsync()
    {
        if (IsBuiltInOnly)
        {
            _smtcService.Stop();
            return;
        }
        await _gsmtcService.StopAsync();
    }

    [RelayCommand]
    public async Task SeekToPositionAsync(double seconds)
    {
        _latestRealPositionSeconds = seconds;
        SetSimulatedPosition(seconds);

        // 播放器会先把拖动前的位置回报上来，短暂忽略避免进度条弹回再弹出
        _ignorePositionChangeUntilTicks = Environment.TickCount64 + 1000;

        if (IsBuiltInOnly)
        {
            _smtcService.Seek(TimeSpan.FromSeconds(seconds));
            return;
        }
        await _gsmtcService.ChangePositionAsync(seconds);
    }

    public void Receive(CurrentPositionChangedMessage message)
    {
        if (IsBuiltInOnly) return;
        if (Environment.TickCount64 < _ignorePositionChangeUntilTicks) return;

        _latestRealPositionSeconds = message.Value.TotalSeconds;

        // 只有真正的跳转才立即对齐；其余偏差留给 AdvanceSimulatedPosition 用速率修正
        if (Math.Abs(_latestRealPositionSeconds - SimulatedPositionSeconds) >= SeekSnapThresholdSeconds)
        {
            SetSimulatedPosition(_latestRealPositionSeconds);
        }
    }

    public void Receive(CurrentMediaSourceProviderChangedMessage message)
    {
        _appUIThreadProvider.Execute(() =>
        {
            UpdateVolume();
        });
    }
}