using BetterLyrics.WinUI3.Services.Settings;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AlbumArtOverlayViewModel : BaseViewModel
    {
        public AlbumArtOverlayViewModel(ISettingsService settingsService)
            : base(settingsService) { }

        public bool IsCoverOverlayEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsCoverOverlayEnabled,
                    SettingsDefaultValues.IsCoverOverlayEnabled
                );
            set => Set(SettingsKeys.IsCoverOverlayEnabled, value);
        }
        public bool IsDynamicCoverOverlay
        {
            get =>
                Get(
                    SettingsKeys.IsDynamicCoverOverlay,
                    SettingsDefaultValues.IsDynamicCoverOverlay
                );
            set => Set(SettingsKeys.IsDynamicCoverOverlay, value);
        }
        public int CoverOverlayOpacity
        {
            get => Get(SettingsKeys.CoverOverlayOpacity, SettingsDefaultValues.CoverOverlayOpacity);
            set => Set(SettingsKeys.CoverOverlayOpacity, value);
        }
        public int CoverOverlayBlurAmount
        {
            get =>
                Get(
                    SettingsKeys.CoverOverlayBlurAmount,
                    SettingsDefaultValues.CoverOverlayBlurAmount
                );
            set => Set(SettingsKeys.CoverOverlayBlurAmount, value);
        }
    }
}
