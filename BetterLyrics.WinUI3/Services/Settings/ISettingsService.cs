using BetterLyrics.WinUI3.Models;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public interface ISettingsService {
        ElementTheme Theme { get; }
        public void SetTheme(ElementTheme theme);

        public List<MusicFolder> MusicLibraries {  get; }
        public void SetMusicLibraries(List<MusicFolder> musicLibraries);

        public Language Language { get; }
        public void SetLanguage(Language language);

        public BackdropType BackdropType { get; }
        public void SetBackdropType(BackdropType backdropType);

        public bool IsCoverOverlayEnabled { get; }
        public void SetIsCoverOverlayEnabled(bool isCoverOverlayEnabled);

        public bool IsDynamicCoverOverlay { get; }
        public void SetDynamicCoverOverlay(bool isDynamicCoverOverlay);

        public int CoverOverlayOpacity {  get; }
        public void SetCoverOverlayOpacity(int overlayOpacity);
    }

}
