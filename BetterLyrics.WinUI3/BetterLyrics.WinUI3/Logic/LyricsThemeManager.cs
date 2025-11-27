using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsThemeManager
    {
        private readonly IMediaSessionsService _mediaSessionsService;

        public LyricsThemeManager(IMediaSessionsService mediaSessionsService)
        {
            _mediaSessionsService = mediaSessionsService;
        }

        public LyricsThemeColors UpdateColors(
            LyricsWindowStatus status,
            Color environmentalColor,
            ValueTransition<Color> accentColor1Transition,
            ValueTransition<Color> accentColor2Transition,
            ValueTransition<Color> accentColor3Transition,
            ValueTransition<Color> accentColor4Transition
            )
        {
            var result = new LyricsThemeColors();

            ElementTheme themeTypeSent;
            if (status.IsAdaptToEnvironment)
            {
                themeTypeSent = Helper.ColorHelper.GetElementThemeFromBackgroundColor(environmentalColor);
            }
            else
            {
                themeTypeSent = status.LyricsBackgroundSettings.LyricsBackgroundTheme;
            }

            bool isLight = themeTypeSent switch
            {
                ElementTheme.Default => Application.Current.RequestedTheme == ApplicationTheme.Light,
                ElementTheme.Light => true,
                ElementTheme.Dark => false,
                _ => false
            };

            Color adaptiveGrayedFontColor;
            Color grayedEnvironmentalColor;
            Color? adaptiveColoredFontColor;

            Color darkColor = Colors.Black;
            Color lightColor = Colors.White;

            if (isLight)
            {
                adaptiveGrayedFontColor = darkColor;
                // brightness = 0.7f;
                grayedEnvironmentalColor = lightColor;

                result.AccentColor1 = _mediaSessionsService.LightAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _mediaSessionsService.LightAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _mediaSessionsService.LightAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _mediaSessionsService.LightAccentColors.ElementAtOrDefault(3);
            }
            else
            {
                adaptiveGrayedFontColor = lightColor;
                // brightness = 0.3f;
                grayedEnvironmentalColor = darkColor;

                result.AccentColor1 = _mediaSessionsService.DarkAccentColors.ElementAtOrDefault(0);
                result.AccentColor2 = _mediaSessionsService.DarkAccentColors.ElementAtOrDefault(1);
                result.AccentColor3 = _mediaSessionsService.DarkAccentColors.ElementAtOrDefault(2);
                result.AccentColor4 = _mediaSessionsService.DarkAccentColors.ElementAtOrDefault(3);
            }

            if (status.IsAdaptToEnvironment)
            {
                adaptiveColoredFontColor = Helper.ColorHelper.GetForegroundColor(environmentalColor);
            }
            else
            {
                if (isLight)
                    adaptiveColoredFontColor = _mediaSessionsService.DarkAccentColors.ElementAtOrDefault(0);
                else
                    adaptiveColoredFontColor = _mediaSessionsService.LightAccentColors.ElementAtOrDefault(0);
            }

            result.ThemeType = themeTypeSent;

            // 背景字色
            switch (status.LyricsStyleSettings.LyricsBgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.BgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.BgFontColor = status.LyricsStyleSettings.LyricsCustomBgFontColor;
                    break;
                default:
                    result.BgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 前景字色
            switch (status.LyricsStyleSettings.LyricsFgFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.FgFontColor = adaptiveColoredFontColor ?? adaptiveGrayedFontColor;
                    break;
                case LyricsFontColorType.Custom:
                    result.FgFontColor = status.LyricsStyleSettings.LyricsCustomFgFontColor;
                    break;
                default:
                    result.FgFontColor = adaptiveGrayedFontColor;
                    break;
            }

            // 描边颜色
            switch (status.LyricsStyleSettings.LyricsStrokeFontColorType)
            {
                case LyricsFontColorType.AdaptiveGrayed:
                    result.StrokeFontColor = grayedEnvironmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.AdaptiveColored:
                    result.StrokeFontColor = environmentalColor.WithBrightness(0.7);
                    break;
                case LyricsFontColorType.Custom:
                    result.StrokeFontColor = status.LyricsStyleSettings.LyricsCustomStrokeFontColor;
                    break;
                default:
                    result.StrokeFontColor = Colors.Transparent;
                    break;
            }

            return result;
        }
    }
}
