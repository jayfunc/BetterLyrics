using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsViewModel : BaseLyricsViewModel, ILyricsViewModel
    {
        public LyricsAlignmentType LyricsAlignmentType
        {
            get =>
                (LyricsAlignmentType)Get(
                    SettingsKeys.DesktopLyricsAlignmentType,
                    SettingsDefaultValues.DesktopLyricsAlignmentType
                );
            set => Set(SettingsKeys.DesktopLyricsAlignmentType, (int)value);
        }
        public int LyricsBlurAmount
        {
            get =>
                Get(
                    SettingsKeys.DesktopLyricsBlurAmount,
                    SettingsDefaultValues.DesktopLyricsBlurAmount
                );
            set => Set(SettingsKeys.DesktopLyricsBlurAmount, value);
        }
        public int LyricsVerticalEdgeOpacity
        {
            get =>
                Get(
                    SettingsKeys.DesktopLyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.DesktopLyricsVerticalEdgeOpacity
                );
            set => Set(SettingsKeys.DesktopLyricsVerticalEdgeOpacity, value);
        }
        public float LyricsLineSpacingFactor
        {
            get =>
                Get(
                    SettingsKeys.DesktopLyricsLineSpacingFactor,
                    SettingsDefaultValues.DesktopLyricsLineSpacingFactor
                );
            set
            {
                Set(SettingsKeys.DesktopLyricsLineSpacingFactor, value);
                WeakReferenceMessenger.Default.Send(new DesktopLyricsRelayoutRequestedMessage());
            }
        }
        public int LyricsFontSize
        {
            get =>
                Get(
                    SettingsKeys.DesktopLyricsFontSize,
                    SettingsDefaultValues.DesktopLyricsFontSize
                );
            set
            {
                Set(SettingsKeys.DesktopLyricsFontSize, value);
                WeakReferenceMessenger.Default.Send(new DesktopLyricsRelayoutRequestedMessage());
            }
        }
        public bool IsLyricsGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsDesktopLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsDesktopLyricsGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsDesktopLyricsGlowEffectEnabled, value);
        }
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsDesktopLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsDesktopLyricsDynamicGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsDesktopLyricsDynamicGlowEffectEnabled, value);
        }
        public LyricsFontColorType LyricsFontColorType
        {
            get =>
                (LyricsFontColorType)Get(
                    SettingsKeys.DesktopLyricsFontColorType,
                    SettingsDefaultValues.DesktopLyricsFontColorType
                );
            set
            {
                Set(SettingsKeys.DesktopLyricsFontColorType, (int)value);
                WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
            }
        }
        public int LyricsFontSelectedAccentColorIndex
        {
            get =>
                Get(
                    SettingsKeys.DesktopLyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.DesktopLyricsFontSelectedAccentColorIndex
                );
            set
            {
                if (value >= 0)
                {
                    Set(SettingsKeys.DesktopLyricsFontSelectedAccentColorIndex, value);
                    WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
                }
            }
        }

        public DesktopLyricsViewModel(ISettingsService settingsService)
            : base(settingsService) { }
    }
}
