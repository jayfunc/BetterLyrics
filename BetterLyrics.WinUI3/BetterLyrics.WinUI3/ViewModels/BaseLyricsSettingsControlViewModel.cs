using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public abstract partial class BaseLyricsSettingsControlViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<Color> CoverImageDominantColors { get; set; }

        public abstract LyricsAlignmentType LyricsAlignmentType { get; set; }

        public abstract int LyricsBlurAmount { get; set; }

        public abstract int LyricsVerticalEdgeOpacity { get; set; }

        public abstract float LyricsLineSpacingFactor { get; set; }

        public abstract int LyricsFontSize { get; set; }

        public abstract bool IsLyricsGlowEffectEnabled { get; set; }

        public abstract bool IsLyricsDynamicGlowEffectEnabled { get; set; }

        public abstract LyricsFontColorType LyricsFontColorType { get; set; }

        public abstract int LyricsFontSelectedAccentColorIndex { get; set; }

        private readonly IPlaybackService _playbackService;

        public BaseLyricsSettingsControlViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
            _playbackService = playbackService;

            _playbackService.SongInfoChanged += (s, e) =>
            {
                CoverImageDominantColors =
                    _playbackService.SongInfo?.CoverImageDominantColors ?? [];
            };

            CoverImageDominantColors = _playbackService.SongInfo?.CoverImageDominantColors ?? [];
        }
    }
}
