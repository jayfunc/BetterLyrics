using System.Collections.ObjectModel;
using System.Linq;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<Color> _coverImageDominantColors =
        [
            .. Enumerable.Repeat(Colors.Transparent, ImageHelper.AccentColorCount),
        ];

        public int LyricsAlignmentType
        {
            get => Get(SettingsKeys.LyricsAlignmentType, SettingsDefaultValues.LyricsAlignmentType);
            set => Set(SettingsKeys.LyricsAlignmentType, value);
        }
        public int LyricsBlurAmount
        {
            get => Get(SettingsKeys.LyricsBlurAmount, SettingsDefaultValues.LyricsBlurAmount);
            set => Set(SettingsKeys.LyricsBlurAmount, value);
        }
        public int LyricsVerticalEdgeOpacity
        {
            get =>
                Get(
                    SettingsKeys.LyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.LyricsVerticalEdgeOpacity
                );
            set => Set(SettingsKeys.LyricsVerticalEdgeOpacity, value);
        }
        public float LyricsLineSpacingFactor
        {
            get =>
                Get(
                    SettingsKeys.LyricsLineSpacingFactor,
                    SettingsDefaultValues.LyricsLineSpacingFactor
                );
            set
            {
                Set(SettingsKeys.LyricsLineSpacingFactor, value);
                WeakReferenceMessenger.Default.Send(new LyricsRelayoutRequestedMessage());
            }
        }
        public int LyricsFontSize
        {
            get => Get(SettingsKeys.LyricsFontSize, SettingsDefaultValues.LyricsFontSize);
            set
            {
                Set(SettingsKeys.LyricsFontSize, value);
                WeakReferenceMessenger.Default.Send(new LyricsRelayoutRequestedMessage());
            }
        }
        public bool IsLyricsGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsLyricsGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsLyricsGlowEffectEnabled, value);
        }
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsLyricsDynamicGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsLyricsDynamicGlowEffectEnabled, value);
        }
        public int LyricsFontColorType
        {
            get => Get(SettingsKeys.LyricsFontColorType, SettingsDefaultValues.LyricsFontColorType);
            set
            {
                Set(SettingsKeys.LyricsFontColorType, value);
                WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
            }
        }
        public int LyricsFontSelectedAccentColorIndex
        {
            get =>
                Get(
                    SettingsKeys.LyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.LyricsFontSelectedAccentColorIndex
                );
            set
            {
                if (value >= 0)
                {
                    Set(SettingsKeys.LyricsFontSelectedAccentColorIndex, value);
                    WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
                }
            }
        }

        public LyricsViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            WeakReferenceMessenger.Default.Register<LyricsViewModel, SongInfoChangedMessage>(
                this,
                async (r, m) =>
                {
                    if (m.Value?.AlbumArt == null)
                        CoverImageDominantColors =
                        [
                            .. Enumerable.Repeat(Colors.Transparent, ImageHelper.AccentColorCount),
                        ];
                    else
                    {
                        CoverImageDominantColors =
                        [
                            .. await ImageHelper.GetAccentColorsFromByte(m.Value.AlbumArt),
                        ];
                    }
                }
            );
        }
    }
}
