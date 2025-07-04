// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel : BaseViewModel
    {
        private SoftwareBitmap? _lastAlbumArtSwBitmap = null;
        private SoftwareBitmap? _albumArtSwBitmap = null;

        private float _albumArtSize = 0f;
        private int _albumArtCornerRadius = 0;

        private float _albumArtY = 0f;

        private string? _lastSongTitle;
        private string? _songTitle;

        private string? _lastSongArtist;
        private string? _songArtist;

        private float _canvasWidth = 0f;
        private float _canvasHeight = 0f;

        private readonly float _defaultOpacity = 0.3f;
        private readonly float _highlightedOpacity = 1.0f;

        private readonly float _defaultScale = 0.75f;
        private readonly float _highlightedScale = 1.0f;

        private readonly float _coverRotateSpeed = 0.003f;
        private float _rotateAngle = 0f;

        private readonly float _lyricsGlowEffectAmount = 8f;

        private float _maxLyricsWidth = 0f;

        private readonly IMusicSearchService _musicSearchService;
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;

        private readonly float _leftMargin = 36f;
        private readonly float _middleMargin = 36f;
        private readonly float _rightMargin = 36f;
        private readonly float _topMargin = 36f;
        private readonly float _bottomMargin = 36f;

        private Color _fontColor;
        private Color? _adaptiveFontColor = null;
        private Color? _customFontColor;
        private Color _lightFontColor = Colors.White;
        private Color _darkFontColor = Colors.Black;
        private Color? _albumArtAccentColor = null;
        private Color _lyricsWindowBgColor = Colors.Transparent;

        private int _startVisibleLineIndex = -1;
        private int _endVisibleLineIndex = -1;

        private bool _isDebugOverlayEnabled = false;
        private bool _isDesktopMode = false;
        private bool _isDockMode = false;
        private bool _isFanLyricsEnabled = false;

        private bool _isPlaying = true;

        private bool _isRelayoutNeeded = true;

        private int _langIndex = 0;

        private List<List<LyricsLine>> _multiLangLyrics = [];

        private CanvasTextFormat _lyricsTextFormat = new()
        {
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            VerticalAlignment = CanvasVerticalAlignment.Top,
        };
        private CanvasTextFormat _titleTextFormat = new()
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            WordWrapping = CanvasWordWrapping.Wrap,
        };
        private CanvasTextFormat _artistTextFormat = new()
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = CanvasHorizontalAlignment.Left,
            WordWrapping = CanvasWordWrapping.Wrap,
        };

        private Task? _refreshLyricsTask;
        private CancellationTokenSource? _refreshLyricsCts;

        public LyricsRendererViewModel(
            ISettingsService settingsService, IPlaybackService playbackService,
            IMusicSearchService musicSearchService, ILibWatcherService libWatcherService) : base(settingsService)
        {
            _musicSearchService = musicSearchService;
            _playbackService = playbackService;
            _libWatcherService = libWatcherService;

            _albumArtCornerRadius = _settingsService.CoverImageRadius;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;

            LyricsFontColorType = _settingsService.LyricsFontColorType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            _customFontColor = _settingsService.LyricsCustomFontColor;

            _titleTextFormat.HorizontalAlignment = _artistTextFormat.HorizontalAlignment = _settingsService.LyricsAlignmentType.ToCanvasHorizontalAlignment();

            _libWatcherService.MusicLibraryFilesChanged +=
                LibWatcherService_MusicLibraryFilesChanged;

            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.PositionChanged += PlaybackService_PositionChanged;

            UpdateFontColor();
        }

        public int CoverOverlayBlurAmount { get; set; }

        public int CoverOverlayOpacity { get; set; }

        private LyricsDisplayType _displayTypeReceived = LyricsDisplayType.PlaceholderOnly;
        private LyricsDisplayType _displayType = LyricsDisplayType.PlaceholderOnly;

        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        public bool IsDynamicCoverOverlayEnabled { get; set; }

        public bool IsLyricsGlowEffectEnabled { get; set; }

        public TextAlignmentType LyricsAlignmentType { get; set; }

        public int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        public LineRenderingType LyricsGlowEffectScope { get; set; }

        [ObservableProperty]
        public partial float LyricsLineSpacingFactor { get; set; }

        public int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeTypeSent { get; set; }

        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

        private int GetCurrentPlayingLineIndex()
        {
            for (int i = 0; i < _multiLangLyrics.SafeGet(_langIndex)?.Count; i++)
            {
                var line = _multiLangLyrics.SafeGet(_langIndex)?[i];
                if (line == null)
                {
                    continue;
                }
                if (
                    line.StartMs <= TotalTime.TotalMilliseconds
                    && TotalTime.TotalMilliseconds <= line.EndMs
                )
                {
                    return i;
                }
            }

            return -1;
        }

        private void GetLinePlayingProgress(LyricsLine line, out int charStartIndex, out int charLength, out float charProgress)
        {
            charStartIndex = 0;
            charLength = 0;
            charProgress = 0f;

            float now = (float)TotalTime.TotalMilliseconds;

            // 1. 还没到本句
            if (now < line.StartMs)
            {
                return;
            }

            // 2. 已经超过本句
            if (now > line.EndMs)
            {
                return;
            }

            // 3. 有逐字时间轴
            if (line.CharTimings != null && line.CharTimings.Count > 0)
            {
                int charTimingsCount = line.CharTimings.Count;
                for (int i = 0; i < charTimingsCount; i++)
                {
                    var timing = line.CharTimings[i];

                    // 当前时间在某个字的高亮区间
                    if (now >= timing.StartMs && now <= timing.EndMs)
                    {
                        charStartIndex = timing.StartIndex;
                        charLength = timing.Text.Length;
                        if (timing.EndMs != timing.StartMs)
                        {
                            charProgress = (now - timing.StartMs) / (timing.EndMs - timing.StartMs);
                        }
                        else
                        {
                            charProgress = 0f;
                        }
                        return;
                    }
                }
            }
            else
            {
                // 没有逐字时间轴，直接线性
                charProgress = (now - line.StartMs) / line.DurationMs;
                charProgress = Math.Clamp(charProgress, 0f, 1f);
                charStartIndex = 0;
                charLength = line.Text.Length;
            }
        }

        private Tuple<int, int> GetMaxLyricsLineIndexBoundaries()
        {
            if (
                SongInfo == null
                || _multiLangLyrics.SafeGet(_langIndex) == null
                || _multiLangLyrics[_langIndex].Count == 0
            )
            {
                return new Tuple<int, int>(-1, -1);
            }

            return new Tuple<int, int>(0, _multiLangLyrics[_langIndex].Count - 1);
        }

        private async void LibWatcherService_MusicLibraryFilesChanged(object? sender, LibChangedEventArgs e)
        {
            await RefreshLyricsAsync();
        }

        private void PlaybackService_IsPlayingChanged(object? sender, IsPlayingChangedEventArgs e)
        {
            _isPlaying = e.IsPlaying;
        }

        private void PlaybackService_PositionChanged(object? sender, PositionChangedEventArgs e)
        {
            TotalTime = e.Position;
        }

        private async void PlaybackService_SongInfoChanged(object? sender, SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
            if (SongInfo?.Title != _songTitle || SongInfo?.Artist != _songArtist)
            {
                _lastSongTitle = _songTitle;
                _songTitle = SongInfo?.Title;

                _lastSongArtist = _songArtist;
                _songArtist = SongInfo?.Artist;

                _songInfoOpacityTransition.Reset(0f);
                _songInfoOpacityTransition.StartTransition(1f);

                await RefreshLyricsAsync();

                TotalTime = TimeSpan.Zero;
            }

            if (SongInfo?.AlbumArtSwBitmap != _albumArtSwBitmap)
            {
                _lastAlbumArtSwBitmap = _albumArtSwBitmap;
                _albumArtSwBitmap = SongInfo?.AlbumArtSwBitmap;

                _albumArtAccentColor = SongInfo?.AlbumArtAccentColor;
                _lyricsWindowBgColor = _albumArtAccentColor ?? Colors.Gray;

                _albumArtBgTransition.Reset(0f);
                _albumArtBgTransition.StartTransition(1f);

                if (!_isDesktopMode && !_isDockMode) _adaptiveFontColor = Helper.ColorHelper.GetForegroundColor(_lyricsWindowBgColor);
                UpdateFontColor();
            }
        }

        private async Task RefreshLyricsAsync()
        {
            // 取消上一次
            _refreshLyricsCts?.Cancel();
            if (_refreshLyricsTask != null)
            {
                await _refreshLyricsTask;
            }

            var cts = new CancellationTokenSource();
            _refreshLyricsCts = cts;
            var token = cts.Token;

            _refreshLyricsTask = RefreshLyricsCoreAsync(token);
            await _refreshLyricsTask;
        }

        private async Task RefreshLyricsCoreAsync(CancellationToken token)
        {
            try
            {
                SetLyricsLoadingPlaceholder();

                string? lyricsRaw = null;
                LyricsFormat? lyricsFormat = null;

                if (SongInfo != null)
                {
                    (lyricsRaw, lyricsFormat) = await _musicSearchService.SearchLyricsAsync(
                        SongInfo.Title,
                        SongInfo.Artist,
                        SongInfo.Album ?? "",
                        SongInfo.DurationMs ?? 0
                    );
                    token.ThrowIfCancellationRequested();
                }

                _multiLangLyrics = new LyricsParser().Parse(
                    lyricsRaw,
                    lyricsFormat,
                    SongInfo?.Title,
                    SongInfo?.Artist,
                    (int)(SongInfo?.DurationMs ?? 0)
                );
                _isRelayoutNeeded = true;
            }
            catch (Exception) { }
        }

        private void SetLyricsLoadingPlaceholder()
        {
            _multiLangLyrics = [];
            _multiLangLyrics.Add(
                [
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = (int)TimeSpan.FromMinutes(99).TotalMilliseconds,
                        Text = "● ● ●",
                        CharTimings = [],
                    },
                ]
            );
            _isRelayoutNeeded = true;
        }
    }
}
