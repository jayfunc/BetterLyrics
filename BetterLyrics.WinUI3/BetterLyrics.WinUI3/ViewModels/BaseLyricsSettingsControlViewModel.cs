using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseLyricsSettingsControlViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial ObservableCollection<Color> CoverImageDominantColors { get; set; }

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
