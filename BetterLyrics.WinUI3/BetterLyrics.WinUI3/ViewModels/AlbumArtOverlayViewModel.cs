using BetterLyrics.WinUI3.Services.Settings;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AlbumArtOverlayViewModel : BaseSettingsViewModel
    {
        public AlbumArtOverlayViewModel(ISettingsService settingsService)
            : base(settingsService) { }

        private bool? _isCoverOverlayEnabled = null;
        public bool IsCoverOverlayEnabled
        {
            get
            {
                _isCoverOverlayEnabled ??= Get(
                    SettingsKeys.IsCoverOverlayEnabled,
                    SettingsDefaultValues.IsCoverOverlayEnabled
                );
                return _isCoverOverlayEnabled ?? false;
            }
            set
            {
                _isCoverOverlayEnabled = value;
                Set(SettingsKeys.IsCoverOverlayEnabled, value);
            }
        }
        private bool? _isDynamicCoverOverlayEnabled = null;
        public bool IsDynamicCoverOverlayEnabled
        {
            get
            {
                _isDynamicCoverOverlayEnabled ??= Get(
                    SettingsKeys.IsDynamicCoverOverlayEnabled,
                    SettingsDefaultValues.IsDynamicCoverOverlayEnabled
                );
                return _isDynamicCoverOverlayEnabled ?? false;
            }
            set
            {
                _isDynamicCoverOverlayEnabled = value;
                Set(SettingsKeys.IsDynamicCoverOverlayEnabled, value);
            }
        }
        private int? _coverOverlayOpacity;
        public int CoverOverlayOpacity
        {
            get
            {
                _coverOverlayOpacity ??= Get(
                    SettingsKeys.CoverOverlayOpacity,
                    SettingsDefaultValues.CoverOverlayOpacity
                );
                return _coverOverlayOpacity ?? 0;
            }
            set
            {
                _coverOverlayOpacity = value;
                Set(SettingsKeys.CoverOverlayOpacity, value);
            }
        }
        private int? _coverOverlayBlurAmount;
        public int CoverOverlayBlurAmount
        {
            get
            {
                _coverOverlayBlurAmount ??= Get(
                    SettingsKeys.CoverOverlayBlurAmount,
                    SettingsDefaultValues.CoverOverlayBlurAmount
                );
                return _coverOverlayBlurAmount ?? 0;
            }
            set
            {
                _coverOverlayBlurAmount = value;
                Set(SettingsKeys.CoverOverlayBlurAmount, value);
            }
        }
    }
}
