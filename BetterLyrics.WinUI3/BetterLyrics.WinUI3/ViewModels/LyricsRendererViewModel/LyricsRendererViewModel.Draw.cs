using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Numerics;
using System.Windows.Media.Media3D;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsPureColorOverlayEnabled)
            {
                if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
                {
                    FillBackground(ds, _immersiveBgColorTransition.Value, 0f,
                        _immersiveBgOpacityTransition.Value * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100f);
                }
                else
                {
                    FillBackground(ds, _albumArtAccentColor1Transition.Value, 0f,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100.0);
                }
            }
            DrawFluidBackground(control, ds);
            DrawSpectrum(control, ds);

            DrawSnowEffect(ds);

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled)
            {
                // Blurred lyrics layer
                using var blurredLyrics = new CanvasCommandList(control);
                using (var blurredLyricsDs = blurredLyrics.CreateDrawingSession())
                {
                    DrawLyrics(control, blurredLyricsDs);
                }
                ds.DrawImage(new Transform3DEffect
                {
                    Source = blurredLyrics,
                    TransformMatrix = _lyrics3DMatrix
                });
            }
            else
            {
                DrawLyrics(control, ds);
            }

            DrawFogEffect(ds);
            //DrawRaindropEffect(ds, combined);

            if (_isDebugOverlayEnabled)
            {
                _drawFrameCount++;

                var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

                if (currentPlayingLine != null)
                {
                    GetLinePlayingProgress(
                        _playingLineIndex,
                        out int syllableStartIndex,
                        out int syllableLength,
                        out double syllableProgress
                    );

                    ds.DrawText(
                        $"[DEBUG]\n" +
                            $"Canvas size: {_canvasWidth}x{_canvasHeight}\n" +
                            $"FPS (Draw): {_displayedDrawFrameCount}\n" +
                            $"Playing line: {_playingLineIndex}\n" +
                            $"Syllable start idx: {syllableStartIndex}\n" +
                            $"Syllable len: {syllableLength}\n" +
                            $"Syllable prog: {syllableProgress}\n" +
                            $"Visible lines: [{_startVisibleLineIndex}, {_endVisibleLineIndex}]\n" +
                            $"Total line count: {GetMaxLyricsLineIndexBoundaries().Item2 + 1}\n" +
                            $"Cur time: {TotalTime + TimeSpan.FromMilliseconds(_mediaSessionsService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0)}\n" +
                            $"Song duration: {TimeSpan.FromMilliseconds(_mediaSessionsService.CurrentSongInfo?.DurationMs ?? 0)}\n" +
                            $"Y offset: {_canvasYScrollTransition.Value}",
                        new Vector2(10, 40),
                        ThemeTypeSent == Microsoft.UI.Xaml.ElementTheme.Light ? Colors.Black : Colors.White,
                        _debugTextFormat
                    );
                }

                if (_drawFrameStopwatch?.Elapsed.TotalSeconds >= 1.0)
                {
                    _displayedDrawFrameCount = _drawFrameCount;
                    _drawFrameStopwatch?.Restart();
                    _drawFrameCount = 0;
                }
            }
        }

        public void DrawSpectrum(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_spectrumGeometry != null)
            {
                var gradientStops = new CanvasGradientStop[]
                 {
                    new() { Position = 0.0f, Color = Colors.Transparent },
                    new() { Position = 0.7f, Color = Colors.Transparent },
                    new() { Position = 1.0f, Color = _adaptiveColoredFontColor ?? _albumArtAccentColor1Transition.Value }
                 };

                using var gradientBrush = new CanvasLinearGradientBrush(ds, gradientStops);

                switch (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.SpectrumPlacement)
                {
                    case SpectrumPlacement.Top:
                        gradientBrush.StartPoint = new Vector2(0, (float)_canvasHeight);
                        gradientBrush.EndPoint = new Vector2(0, 0);
                        break;
                    case SpectrumPlacement.Bottom:
                        gradientBrush.StartPoint = new Vector2(0, 0);
                        gradientBrush.EndPoint = new Vector2(0, (float)_canvasHeight);
                        break;
                    default:
                        break;
                }


                // 使用渐变画刷填充
                ds.FillGeometry(_spectrumGeometry, gradientBrush);

                // 纯色
                //ds.FillGeometry(geometry, _adaptiveColoredFontColor ?? _albumArtAccentColor1Transition.Value);

                // 绘制轮廓线
                //var lineColor = Colors.SkyBlue;
                //float strokeWidth = 2f;
                //ds.DrawGeometry(geometry, _albumArtAccentColor4Transition.Value, strokeWidth);
            }
        }

        private void DrawFluidBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_fluidEffect != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsFluidOverlayEnabled)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _fluidEffect,
                    Opacity = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.FluidOverlayOpacity / 100f
                });
            }
        }

        private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null)
            {
                return;
            }

            var lyricsEffectSettings = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings;

            var rotationY = currentPlayingLine.OriginalPosition.WithX(lyricsEffectSettings.FanLyricsAngle < 0 ? (float)_maxLyricsWidth : 0);

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;

                var textLayout = line.OriginalCanvasTextLayout;
                if (textLayout == null) continue;

                double layoutWidth = (double)textLayout.LayoutBounds.Width;
                double layoutHeight = (double)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0) continue;

                double yOffset = line.YOffsetTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;

                //// 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition) *
                    Matrix3x2.CreateRotation((float)line.AngleTransition.Value, rotationY) *
                    Matrix3x2.CreateTranslation((float)_lyricsX, (float)yOffset);

                using var textOnlyLayer = new CanvasCommandList(control);
                using (var textOnlyLayerDs = textOnlyLayer.CreateDrawingSession())
                {
                    var strokeWidth = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth;

                    // 描边
                    if (strokeWidth > 0)
                    {
                        if (line.PhoneticCanvasGeometry != null)
                        {
                            textOnlyLayerDs.DrawGeometry(line.PhoneticCanvasGeometry, line.PhoneticPosition, _strokeFontColor, strokeWidth);
                        }
                        if (line.OriginalCanvasGeometry != null)
                        {
                            textOnlyLayerDs.DrawGeometry(line.OriginalCanvasGeometry, line.OriginalPosition, _strokeFontColor, strokeWidth);
                        }
                        if (line.TranslatedCanvasGeometry != null)
                        {
                            textOnlyLayerDs.DrawGeometry(line.TranslatedCanvasGeometry, line.TranslatedPosition, _strokeFontColor, strokeWidth);
                        }
                    }

                    // 绘制文本（填充）
                    if (line.PhoneticCanvasTextLayout != null)
                    {
                        textOnlyLayerDs.DrawTextLayout(line.PhoneticCanvasTextLayout, line.PhoneticPosition, _bgFontColor);
                    }
                    if (line.OriginalCanvasTextLayout != null)
                    {
                        textOnlyLayerDs.DrawTextLayout(line.OriginalCanvasTextLayout, line.OriginalPosition, _bgFontColor);
                    }
                    if (line.TranslatedCanvasTextLayout != null)
                    {
                        textOnlyLayerDs.DrawTextLayout(line.TranslatedCanvasTextLayout, line.TranslatedPosition, _bgFontColor);
                    }
                }

                if (i == _playingLineIndex)
                {
                    DrawPlayingLine(control, ds, textOnlyLayer, line, i);
                }
                else
                {
                    DrawUnPlayingLine(ds, textOnlyLayer, line);
                }

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawPlayingLine(ICanvasAnimatedControl control, CanvasDrawingSession ds, ICanvasImage textOnlyLayer, LyricsLine line, int lineIndex)
        {
            var lyricsEffectSettings = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings;

            var lineBlurAmount = line.BlurAmountTransition.Value;

            var phoneticTextOpacity = line.PhoneticTextOpacityTransition.Value;
            var originalTextOpacity = line.OriginalTextOpacityTransition.Value;
            var translatedTextOpacity = line.TranslatedTextOpacityTransition.Value;

            var phoneticTextLayout = line.PhoneticCanvasTextLayout;
            if (phoneticTextLayout != null)
            {
                Rect bounds = phoneticTextLayout.LayoutBounds;

                var rect = new Rect(
                    bounds.X + line.PhoneticPosition.X,
                    bounds.Y + line.PhoneticPosition.Y,
                    bounds.Width,
                    bounds.Height
                );
                ds.DrawImage(new GaussianBlurEffect
                {
                    BlurAmount = (float)lineBlurAmount,
                    Source = textOnlyLayer,
                    BorderMode = EffectBorderMode.Soft
                }, rect, rect, (float)phoneticTextOpacity);
            }

            var originalTextLayout = line.OriginalCanvasTextLayout;
            if (originalTextLayout != null)
            {
                GetLinePlayingProgress(lineIndex, out int syllableStartIndex, out int syllableLength, out double syllableProgress);

                var curCharIndex = syllableStartIndex + syllableLength * syllableProgress; // 当前唱的字符（相对于整行歌词，非子行）
                float fadeWidth = (1f / line.OriginalText.Length) * 0.5f; // 渐变边缘宽度：半个字

                var lineRegions = originalTextLayout.GetCharacterRegions(0, line.OriginalText.Length);
                var charCountUpToCurRegion = 0;
                foreach (var subLineRegion in lineRegions)
                {
                    var subLineLayoutBounds = subLineRegion.LayoutBounds;
                    charCountUpToCurRegion += subLineRegion.CharacterCount;

                    Rect subLineRect = new Rect(
                        subLineLayoutBounds.X + line.OriginalPosition.X,
                        subLineLayoutBounds.Y + line.OriginalPosition.Y,
                        subLineLayoutBounds.Width,
                        subLineLayoutBounds.Height
                    );

                    using (var maskLayer = new CanvasCommandList(control))
                    {
                        using (var maskLayerDs = maskLayer.CreateDrawingSession())
                        {
                            float currentPos = (float)((curCharIndex - subLineRegion.CharacterIndex) / subLineRegion.CharacterCount);
                            currentPos = Math.Clamp(currentPos, 0, 1 + fadeWidth);

                            using (var maskBrush = new CanvasLinearGradientBrush(ds,
                            [
                                new CanvasGradientStop { Position = 0, Color = Colors.White.WithAlpha((byte)(255 * originalTextOpacity)) }, // 左侧：亮
                                new CanvasGradientStop { Position = currentPos, Color = Colors.White.WithAlpha((byte)(255 * originalTextOpacity)) },
                                new CanvasGradientStop { Position = currentPos + fadeWidth, Color = Color.FromArgb((byte)(255 * Math.Min(0.3, originalTextOpacity)), 255, 255, 255) }, // 过渡到暗
                                new CanvasGradientStop { Position = 1 + fadeWidth, Color = Color.FromArgb((byte)(255 * Math.Min(0.3, originalTextOpacity)), 255, 255, 255) } // 右侧：暗
                            ]))
                            {
                                if (maskBrush != null)
                                {
                                    maskBrush.StartPoint = new Vector2((float)subLineRect.X, (float)subLineRect.Y);
                                    maskBrush.EndPoint = new Vector2((float)(subLineRect.X + subLineRect.Width), (float)subLineRect.Y);

                                    maskLayerDs.FillRectangle(subLineRect, maskBrush);
                                }
                            }
                        }

                        using var textWithOpacityLayer = new AlphaMaskEffect
                        {
                            Source = new CropEffect
                            {
                                Source = textOnlyLayer,
                                SourceRectangle = subLineRect,
                                BorderMode = EffectBorderMode.Soft,
                            },
                            AlphaMask = maskLayer,
                        };

                        for (int i = subLineRegion.CharacterIndex; i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount; i++)
                        {
                            int curCharIndexInt = (int)Math.Floor(curCharIndex);

                            var charRegions = originalTextLayout.GetCharacterRegions(i, 1);
                            if (charRegions.Length > 0)
                            {
                                // START 处理浮动动画
                                double floatOffset = 0;
                                double targetFloatOffset = 2;
                                if (lyricsEffectSettings.IsLyricsFloatAnimationEnabled)
                                {
                                    if (i < curCharIndexInt)
                                    {
                                        floatOffset = 0;
                                    }
                                    else if (i == curCharIndexInt)
                                    {
                                        var charProgress = curCharIndex - curCharIndexInt;
                                        floatOffset = -targetFloatOffset + charProgress * targetFloatOffset;
                                    }
                                    else
                                    {
                                        floatOffset = -targetFloatOffset;
                                    }
                                }
                                // END 处理浮动动画

                                var charRegion = charRegions.FirstOrDefault();
                                var charLayoutBounds = charRegion.LayoutBounds;

                                var sourceCharRect = new Rect(
                                    charLayoutBounds.X + line.OriginalPosition.X,
                                    charLayoutBounds.Y + line.OriginalPosition.Y,
                                    charLayoutBounds.Width,
                                    charLayoutBounds.Height
                                );

                                // START 处理缩放、辉光动画
                                double scale = 1;
                                double glow = 0;
                                var parentSyllable = line.LyricsSyllables.FirstOrDefault(x => x.StartIndex <= i && i < x.StartIndex + x.Text.Length);
                                if (parentSyllable != null && parentSyllable.IsLongDuration && parentSyllable.StartIndex == syllableStartIndex)
                                {
                                    if (lyricsEffectSettings.IsLyricsScaleEffectEnabled)
                                    {
                                        scale += Math.Sin(syllableProgress * Math.PI) * 0.15;
                                    }
                                    if (lyricsEffectSettings.IsLyricsGlowEffectEnabled)
                                    {
                                        glow = Math.Sin(syllableProgress * Math.PI) * 8;
                                    }
                                }
                                // END 处理缩放、辉光动画

                                using (var charWithOpacityLayer = new CropEffect
                                {
                                    Source = textWithOpacityLayer,
                                    SourceRectangle = sourceCharRect,
                                    BorderMode = EffectBorderMode.Soft,
                                })
                                {
                                    var destCharRect = sourceCharRect.Scale(scale).AddY(-floatOffset);

                                    if (glow > 0)
                                    {
                                        ds.DrawImage(new GaussianBlurEffect
                                        {
                                            Source = charWithOpacityLayer,
                                            BlurAmount = (float)glow,
                                            BorderMode = EffectBorderMode.Soft,
                                        }, destCharRect.Extend(16), sourceCharRect.Extend(16));
                                    }
                                    ds.DrawImage(charWithOpacityLayer, destCharRect, sourceCharRect);
                                }

                            }
                        }
                    }
                }
            }

            var translatedTextLayout = line.TranslatedCanvasTextLayout;
            if (translatedTextLayout != null)
            {
                Rect bounds = translatedTextLayout.LayoutBounds;

                var rect = new Rect(
                    bounds.X + line.TranslatedPosition.X,
                    bounds.Y + line.TranslatedPosition.Y,
                    bounds.Width,
                    bounds.Height
                );
                ds.DrawImage(new GaussianBlurEffect
                {
                    BlurAmount = (float)lineBlurAmount,
                    Source = textOnlyLayer,
                    BorderMode = EffectBorderMode.Soft
                }, rect, rect, (float)translatedTextOpacity);
            }
        }

        private void DrawUnPlayingLine(CanvasDrawingSession ds, ICanvasImage textOnlyLayer, LyricsLine line)
        {
            var lineBlurAmount = line.BlurAmountTransition.Value;

            var phoneticTextOpacity = line.PhoneticTextOpacityTransition.Value;
            var originalTextOpacity = line.OriginalTextOpacityTransition.Value;
            var translatedTextOpacity = line.TranslatedTextOpacityTransition.Value;

            var phoneticTextLayout = line.PhoneticCanvasTextLayout;
            if (phoneticTextLayout != null)
            {
                Rect bounds = phoneticTextLayout.LayoutBounds;

                var rect = new Rect(
                    bounds.X + line.PhoneticPosition.X,
                    bounds.Y + line.PhoneticPosition.Y,
                    bounds.Width,
                    bounds.Height
                );
                ds.DrawImage(new GaussianBlurEffect
                {
                    BlurAmount = (float)lineBlurAmount,
                    Source = textOnlyLayer,
                    BorderMode = EffectBorderMode.Soft
                }, rect, rect, (float)phoneticTextOpacity);
            }

            var originalTextLayout = line.OriginalCanvasTextLayout;
            if (originalTextLayout != null)
            {
                Rect bounds = originalTextLayout.LayoutBounds;

                var rect = new Rect(
                    bounds.X + line.OriginalPosition.X,
                    bounds.Y + line.OriginalPosition.Y,
                    bounds.Width,
                    bounds.Height
                );
                ds.DrawImage(new GaussianBlurEffect
                {
                    BlurAmount = (float)lineBlurAmount,
                    Source = textOnlyLayer,
                    BorderMode = EffectBorderMode.Soft
                }, rect, rect, (float)originalTextOpacity);
            }

            var translatedTextLayout = line.TranslatedCanvasTextLayout;
            if (translatedTextLayout != null)
            {
                Rect bounds = translatedTextLayout.LayoutBounds;

                var rect = new Rect(
                    bounds.X + line.TranslatedPosition.X,
                    bounds.Y + line.TranslatedPosition.Y,
                    bounds.Width,
                    bounds.Height
                );
                ds.DrawImage(new GaussianBlurEffect
                {
                    BlurAmount = (float)lineBlurAmount,
                    Source = textOnlyLayer,
                    BorderMode = EffectBorderMode.Soft
                }, rect, rect, (float)translatedTextOpacity);
            }
        }

        private void DrawSnowEffect(CanvasDrawingSession ds)
        {
            if (_snowEffect != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsSnowFlakeOverlayEnabled)
            {
                ds.DrawImage(_snowEffect);
            }
        }

        private void DrawFogEffect(CanvasDrawingSession ds)
        {
            if (_fogEffect != null && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsFogOverlayEnabled)
            {
                ds.DrawImage(_fogEffect);
            }
        }

        private void DrawRaindropEffect(CanvasDrawingSession ds, IGraphicsEffectSource source)
        {
            if (_raindropEffect != null)
            {
                _raindropEffect.Sources[0] = source;
                ds.DrawImage(_raindropEffect);
            }
        }

        private void FillBackground(CanvasDrawingSession ds, Color color, double radius, double opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                (float)radius,
                (float)radius,
                color.WithAlpha((byte)(opacity * 255))
            );
        }

    }
}
