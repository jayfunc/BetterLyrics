// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    /// <summary>
    /// Defines the <see cref="LyricsSettingsControlViewModel" />
    /// </summary>
    public partial class LyricsSettingsControlViewModel : BaseViewModel
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsSettingsControlViewModel"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        public LyricsSettingsControlViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            IsActive = true;

            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            LyricsFontColorType = _settingsService.LyricsFontColorType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether IsLyricsGlowEffectEnabled
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsGlowEffectEnabled { get; set; }

        /// <summary>
        /// Gets or sets the LyricsAlignmentType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsAlignmentType LyricsAlignmentType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsBlurAmount
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBlurAmount { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontColorType
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontSize
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontSize { get; set; }

        /// <summary>
        /// Gets or sets the LyricsFontWeight
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        /// <summary>
        /// Gets or sets the LyricsGlowEffectScope
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LineRenderingType LyricsGlowEffectScope { get; set; }

        /// <summary>
        /// Gets or sets the LyricsLineSpacingFactor
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        /// <summary>
        /// Gets or sets the LyricsVerticalEdgeOpacity
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The OnIsLyricsGlowEffectEnabledChanged
        /// </summary>
        /// <param name="value">The value<see cref="bool"/></param>
        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsLyricsGlowEffectEnabled = value;
        }

        /// <summary>
        /// The OnLyricsAlignmentTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsAlignmentType"/></param>
        partial void OnLyricsAlignmentTypeChanged(LyricsAlignmentType value)
        {
            _settingsService.LyricsAlignmentType = value;
        }

        /// <summary>
        /// The OnLyricsBlurAmountChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.LyricsBlurAmount = value;
        }

        /// <summary>
        /// The OnLyricsFontColorTypeChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontColorType"/></param>
        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFontColorType = value;
        }

        /// <summary>
        /// The OnLyricsFontSizeChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.LyricsFontSize = value;
        }

        /// <summary>
        /// The OnLyricsFontWeightChanged
        /// </summary>
        /// <param name="value">The value<see cref="LyricsFontWeight"/></param>
        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.LyricsFontWeight = value;
        }

        /// <summary>
        /// The OnLyricsGlowEffectScopeChanged
        /// </summary>
        /// <param name="value">The value<see tef="LyricsGlowEffectScope"/></param>
        partial void OnLyricsGlowEffectScopeChanged(LineRenderingType value)
        {
            _settingsService?.LyricsGlowEffectScope = value;
        }

        /// <summary>
        /// The OnLyricsLineSpacingFactorChanged
        /// </summary>
        /// <param name="value">The value<see cref="float"/></param>
        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }

        /// <summary>
        /// The OnLyricsVerticalEdgeOpacityChanged
        /// </summary>
        /// <param name="value">The value<see cref="int"/></param>
        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }

        #endregion
    }
}
