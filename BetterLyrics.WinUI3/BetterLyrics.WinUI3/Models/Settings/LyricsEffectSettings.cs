using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class LyricsEffectSettings : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsBlurAmount { get; set; } = 5;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsLineFadeEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsGlowEffectEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LineRenderingType LyricsGlowEffectScope { get; set; } = LineRenderingType.CurrentChar;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsGlowEffectAmount { get; set; } = 8;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsShadowEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LineRenderingType LyricsShadowScope { get; set; } = LineRenderingType.LineStartToCurrentChar;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsShadowAmount { get; set; } = 8;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LineRenderingType LyricsHighlightScope { get; set; } = LineRenderingType.LineStartToCurrentChar;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsHighlightAmount { get; set; } = 100; // 100% 是上界
        
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsTranslationHighlightAmount { get; set; } = 60; // 100% 是上界

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLyricsFloatAnimationEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsFloatAmount { get; set; } = 1;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial EasingType LyricsScrollEasingType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDuration { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollTopDelay { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsScrollBottomDelay { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int LyricsVerticalEdgeOpacity { get; set; } = 0; // 0% opacity
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFanLyricsEnabled { get; set; } = false;

        public LyricsEffectSettings(int lyricsScrollTopDuration, int lyricsScrollDuration, int lyricsScrollBottomDuration, EasingType lyricsScrollEasingType)
        {
            LyricsScrollTopDuration = lyricsScrollTopDuration;
            LyricsScrollDuration = lyricsScrollDuration;
            LyricsScrollBottomDuration = lyricsScrollBottomDuration;
            LyricsScrollEasingType = lyricsScrollEasingType;
        }

        public object Clone()
        {
            return new LyricsEffectSettings(this.LyricsScrollTopDuration, this.LyricsScrollDuration, this.LyricsScrollBottomDuration, this.LyricsScrollEasingType)
            {
                LyricsBlurAmount = this.LyricsBlurAmount,
                IsLyricsLineFadeEnabled = this.IsLyricsLineFadeEnabled,
                IsLyricsGlowEffectEnabled = this.IsLyricsGlowEffectEnabled,
                LyricsGlowEffectScope = this.LyricsGlowEffectScope,
                LyricsGlowEffectAmount = this.LyricsGlowEffectAmount,
                IsLyricsShadowEnabled = this.IsLyricsShadowEnabled,
                LyricsShadowScope = this.LyricsShadowScope,
                LyricsShadowAmount = this.LyricsShadowAmount,
                LyricsHighlightScope = this.LyricsHighlightScope,
                LyricsHighlightAmount = this.LyricsHighlightAmount,
                LyricsTranslationHighlightAmount = this.LyricsTranslationHighlightAmount,
                IsLyricsFloatAnimationEnabled = this.IsLyricsFloatAnimationEnabled,
                LyricsFloatAmount = this.LyricsFloatAmount,
                LyricsScrollTopDelay = this.LyricsScrollTopDelay,
                LyricsScrollBottomDelay = this.LyricsScrollBottomDelay,
                LyricsVerticalEdgeOpacity = this.LyricsVerticalEdgeOpacity,
                IsFanLyricsEnabled = this.IsFanLyricsEnabled
            };
        }
    }
}
