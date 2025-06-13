using System.Collections.ObjectModel;
using System.Linq;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Windows.UI;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class InAppLyricsViewModel : BaseLyricsViewModel, ILyricsViewModel
    {
        public LyricsAlignmentType LyricsAlignmentType
        {
            get =>
                (LyricsAlignmentType)Get(
                    SettingsKeys.InAppLyricsAlignmentType,
                    SettingsDefaultValues.InAppLyricsAlignmentType
                );
            set => Set(SettingsKeys.InAppLyricsAlignmentType, (int)value);
        }
        public int LyricsBlurAmount
        {
            get =>
                Get(
                    SettingsKeys.InAppLyricsBlurAmount,
                    SettingsDefaultValues.InAppLyricsBlurAmount
                );
            set => Set(SettingsKeys.InAppLyricsBlurAmount, value);
        }
        public int LyricsVerticalEdgeOpacity
        {
            get =>
                Get(
                    SettingsKeys.InAppLyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.InAppLyricsVerticalEdgeOpacity
                );
            set => Set(SettingsKeys.InAppLyricsVerticalEdgeOpacity, value);
        }
        public float LyricsLineSpacingFactor
        {
            get =>
                Get(
                    SettingsKeys.InAppLyricsLineSpacingFactor,
                    SettingsDefaultValues.InAppLyricsLineSpacingFactor
                );
            set
            {
                Set(SettingsKeys.InAppLyricsLineSpacingFactor, value);
                WeakReferenceMessenger.Default.Send(new InAppLyricsRelayoutRequestedMessage());
            }
        }
        public int LyricsFontSize
        {
            get => Get(SettingsKeys.InAppLyricsFontSize, SettingsDefaultValues.InAppLyricsFontSize);
            set
            {
                Set(SettingsKeys.InAppLyricsFontSize, value);
                WeakReferenceMessenger.Default.Send(new InAppLyricsRelayoutRequestedMessage());
            }
        }
        public bool IsLyricsGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsInAppLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsInAppLyricsGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsInAppLyricsGlowEffectEnabled, value);
        }
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsInAppLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsInAppLyricsDynamicGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsInAppLyricsDynamicGlowEffectEnabled, value);
        }
        public LyricsFontColorType LyricsFontColorType
        {
            get =>
                (LyricsFontColorType)Get(
                    SettingsKeys.InAppLyricsFontColorType,
                    SettingsDefaultValues.InAppLyricsFontColorType
                );
            set
            {
                Set(SettingsKeys.InAppLyricsFontColorType, (int)value);
                WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
            }
        }
        public int LyricsFontSelectedAccentColorIndex
        {
            get =>
                Get(
                    SettingsKeys.InAppLyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.InAppLyricsFontSelectedAccentColorIndex
                );
            set
            {
                if (value >= 0)
                {
                    Set(SettingsKeys.InAppLyricsFontSelectedAccentColorIndex, value);
                    WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
                }
            }
        }

        public InAppLyricsViewModel(ISettingsService settingsService)
            : base(settingsService) { }
    }
}
