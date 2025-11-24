using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            // Blurred lyrics layer
            using var blurredLyrics = new CanvasCommandList(control);
            using (var blurredLyricsDs = blurredLyrics.CreateDrawingSession())
            {
                DrawBlurredLyrics(control, blurredLyricsDs);
                //DrawBlurredLyrics2(control, blurredLyricsDs);
            }

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsPureColorOverlayEnabled)
            {
                if (_liveStatesService.LiveStates.LyricsWindowStatus.IsAdaptToEnvironment)
                {
                    FillBackground(combinedDs, _immersiveBgColorTransition.Value, 0f,
                        _immersiveBgOpacityTransition.Value * _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100f);
                }
                else
                {
                    FillBackground(combinedDs, _albumArtAccentColor1Transition.Value, 0f,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.PureColorOverlayOpacity / 100.0);
                }
            }
            DrawAlbumArtBackground(control, combinedDs);
            DrawFluidBackground(control, combinedDs);
            DrawSpectrum(control, combinedDs);

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled)
            {
                combinedDs.DrawImage(new Transform3DEffect
                {
                    Source = blurredLyrics,
                    TransformMatrix = _lyrics3DMatrix
                });
            }
            else
            {
                combinedDs.DrawImage(blurredLyrics);
            }

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawSongInfo(ds);

            DrawSnowEffect(ds);
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
                        out int charStartIndex,
                        out int charLength,
                        out double charProgress
                    );

                    ds.DrawText(
                        $"[DEBUG]\n" +
                            $"Canvas size: {_canvasWidth}x{_canvasHeight}\n" +
                            $"FPS (Draw): {_displayedDrawFrameCount}\n" +
                            $"Playing line: {_playingLineIndex}\n" +
                            $"Syllable start idx: {charStartIndex}\n" +
                            $"Syllable len: {charLength}\n" +
                            $"Syllable prog: {charProgress}\n" +
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

        private void DrawBackgroundImgae(OpacityEffect effect, CanvasDrawingSession ds, CanvasBitmap canvasBitmap)
        {
            double imageWidth = (double)canvasBitmap.Size.Width;
            double imageHeight = (double)canvasBitmap.Size.Height;

            double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
            double scaleFactor = targetSize / Math.Min(imageWidth, imageHeight);

            double x = _canvasWidth / 2 - imageWidth * scaleFactor / 2;
            double y = _canvasHeight / 2 - imageHeight * scaleFactor / 2;

            ds.DrawImage(effect, new Vector2((float)x, (float)y));
        }

        private void DrawAlbumArtBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.IsCoverOverlayEnabled)
            {
                ds.Transform = Matrix3x2.CreateRotation((float)_rotateAngle, control.Size.ToVector2() * 0.5f);

                if (_isAlbumArtBgEffectChanged && _albumArtBgEffect != null)
                {
                    ds.DrawImage(_albumArtBgEffect);
                }
                else if (!_isAlbumArtBgEffectChanged && _albumArtBgRenderTarget != null)
                {
                    double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
                    float offsetX = (float)(_canvasWidth - targetSize) / 2;
                    float offsetY = (float)(_canvasHeight - targetSize) / 2;

                    ds.DrawImage(_albumArtBgRenderTarget, new Vector2(offsetX, offsetY));
                }

                ds.Transform = Matrix3x2.Identity;
            }
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            // 专辑图封面正在变动，需实时绘制
            if (_isAlbumArtEffectChanged && _albumArtEffect != null)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _albumArtEffect,
                    Opacity = (float)_albumArtOpacityTransition.Value
                }, new Vector2((float)_albumArtXTransition.Value, (float)_albumArtYTransition.Value));
            }
            // 专辑图封面不再变动，使用已保存的绘制
            else if (!_isAlbumArtEffectChanged && _albumArtRenderTarget != null)
            {
                // 这里给一个相反的偏移以恢复位置
                ds.DrawImage(new OpacityEffect
                {
                    Source = _albumArtRenderTarget,
                    Opacity = (float)_albumArtOpacityTransition.Value
                }, new Vector2((float)_albumArtXTransition.Value, (float)_albumArtYTransition.Value) -
                    control.Size.ToVector2() / 2 + new Vector2((float)_albumArtSize, (float)_albumArtSize) / 2);
            }
        }

        private void DrawSongInfo(CanvasDrawingSession ds)
        {
            if (_maxSongInfoWidth <= 0)
            {
                return;
            }

            DrawSingleSongInfo(ds, _lastTitleTextLayout, _lastArtistTextLayout, _lastAlbumTextLayout, 1 - _songInfoOpacityTransition.Value);
            DrawSingleSongInfo(ds, _titleTextLayout, _artistTextLayout, _albumTextLayout, _songInfoOpacityTransition.Value);
        }

        private void DrawSingleSongInfo(CanvasDrawingSession ds, CanvasTextLayout? titleLayout, CanvasTextLayout? artistLayout, CanvasTextLayout? albumLayout, double opacity)
        {
            if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowTitle && titleLayout != null)
            {
                double y = _titleYTransition.Value;

                ds.DrawTextLayout(
                    titleLayout,
                    new Vector2((float)_titleXTransition.Value, (float)y),
                    _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));

                y += titleLayout.LayoutBounds.Height;

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowArtists && artistLayout != null)
                {
                    ds.DrawTextLayout(
                        artistLayout,
                        new Vector2((float)_titleXTransition.Value, (float)y),
                        _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));

                    y += artistLayout.LayoutBounds.Height;
                }

                if (_liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.ShowAlbum && albumLayout != null)
                {
                    ds.DrawTextLayout(
                        albumLayout,
                        new Vector2((float)_titleXTransition.Value, (float)y),
                        _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 64 * opacity)));
                }
            }
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null)
            {
                return;
            }

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
                    Matrix3x2.CreateRotation(
                        (float)line.AngleTransition.Value,
                        currentPlayingLine.OriginalPosition.WithX(_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.FanLyricsAngle < 0 ? (float)_maxLyricsWidth : 0)) *
                    Matrix3x2.CreateTranslation((float)_lyricsX, (float)yOffset);

                using var combined = new CanvasCommandList(control);
                using var combinedDs = combined.CreateDrawingSession();

                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                using var backgroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor,
                   _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth, _bgFontColor);

                using var backgroundEffect = CanvasHelper.CreateBackgroundEffect(line, backgroundFontEffect, _lyricsOpacityTransition.Value);
                combinedDs.DrawImage(backgroundEffect);

                if (line.HighlightOpacityTransition.Value != 0)
                {
                    GetLinePlayingProgress(i, out int charStartIndex, out int charLength, out double charProgress);

                    using var charMask = CanvasHelper.CreateCharMask(control, line, charStartIndex, charLength, charProgress);
                    using var lineStartToCharMask = CanvasHelper.CreateLineStartToCharMask(control, line, charStartIndex, charLength, charProgress,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsLineFadeEnabled);
                    using var lineMask = CanvasHelper.CreateLineMask(control, line);

                    using var foregroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor,
                        _liveStatesService.LiveStates.LyricsWindowStatus.LyricsStyleSettings.LyricsFontStrokeWidth, _fgFontColor);

                    using var effectLayer = new CanvasCommandList(control);
                    using var effectLayerDs = effectLayer.CreateDrawingSession();
                    if (line.PhoneticText != "" && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.PhoneticLyricsHighlightAmount != 0)
                    {
                        using var phoneticHighlightMask = CanvasHelper.CreatePhoneticHighlightMask(control, line);
                        using var foregroundPhoneticHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, phoneticHighlightMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.PhoneticLyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundPhoneticHighlightEffect);
                    }
                    if (line.TranslatedText != "" && _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.TranslatedLyricsHighlightAmount != 0)
                    {
                        using var translatedHighlightMask = CanvasHelper.CreateTranslatedHighlightMask(control, line);
                        using var foregroundTranslatedHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, translatedHighlightMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.TranslatedLyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundTranslatedHighlightEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsShadowEnabled)
                    {
                        var shadowEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsShadowScope);
                        using var foregroundShadowEffect = CanvasHelper.CreateForegroundShadowEffect(foregroundFontEffect, shadowEffectMask,
                            _albumArtAccentColor1Transition.Value, _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsShadowAmount);
                        effectLayerDs.DrawImage(foregroundShadowEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled)
                    {
                        var blurEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectScope);
                        using var foregroundBlurEffect = CanvasHelper.CreateForegroundBlurEffect(foregroundFontEffect, blurEffectMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsGlowEffectAmount);
                        effectLayerDs.DrawImage(foregroundBlurEffect);
                    }
                    if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightAmount != 0)
                    {
                        var highlightEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightScope);
                        using var foregroundHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, highlightEffectMask,
                            _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.OriginalLyricsHighlightAmount / 100.0);
                        effectLayerDs.DrawImage(foregroundHighlightEffect);
                    }

                    combinedDs.DrawImage(new OpacityEffect
                    {
                        Source = effectLayer,
                        Opacity = (float)Math.Clamp(line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value, 0, 1),
                    });

                    if (i == _playingLineIndex)
                    {
                        if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled)
                        {
                            ds.DrawImage(new DisplacementMapEffect
                            {
                                Source = combined,
                                Displacement = lineStartToCharMask,
                                XChannelSelect = EffectChannelSelect.Red,
                                YChannelSelect = EffectChannelSelect.Alpha,
                                Amount = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsEffectSettings.LyricsFloatAmount,
                            });
                        }
                        else
                        {
                            ds.DrawImage(combined);
                        }
                    }
                    else
                    {
                        ds.DrawImage(combined);
                    }
                }
                else
                {
                    ds.DrawImage(combined);
                }

                // Reset scale
                ds.Transform = Matrix3x2.Identity;
            }
        }

        public void DrawBlurredLyrics2(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _currentLyricsData?.LyricsLines.ElementAtOrDefault(_playingLineIndex);
            if (currentPlayingLine == null) return;

            var settings = _liveStatesService.LiveStates.LyricsWindowStatus;
            var effectSettings = settings.LyricsEffectSettings;
            bool isKaraokeEnabled = effectSettings.IsLyricsLineFadeEnabled;
            var styleSettings = settings.LyricsStyleSettings; // 获取样式设置(描边宽等)

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _currentLyricsData?.LyricsLines.ElementAtOrDefault(i);
                if (line == null || line.OriginalCanvasTextLayout == null) continue;

                var textLayout = line.OriginalCanvasTextLayout;

                // === 1. 恢复您原始的矩阵变换逻辑 ===
                // 不要减去 CenterPosition，保持和您原始代码一致
                double yOffset = line.YOffsetTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;
                float fanAngleX = effectSettings.FanLyricsAngle < 0 ? (float)_maxLyricsWidth : 0;

                ds.Transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition)
                    * Matrix3x2.CreateRotation((float)line.AngleTransition.Value,
                        currentPlayingLine.OriginalPosition.WithX(fanAngleX))
                    * Matrix3x2.CreateTranslation((float)_lyricsX, (float)yOffset);

                // === 2. 坐标关键：必须使用 line.OriginalPosition ===
                // CanvasHelper 里是画在 OriginalPosition 上的，我们这里必须一致
                var drawPos = line.OriginalPosition;

                // A. 绘制阴影 (优化：直接绘制，替代 ShadowEffect)
                if (effectSettings.IsLyricsShadowEnabled)
                {
                    var shadowColor = _albumArtAccentColor1Transition.Value.WithAlpha((byte)(effectSettings.LyricsShadowAmount * 255));
                    // 偏移 2px 绘制阴影
                    ds.DrawTextLayout(textLayout, drawPos.X + 2f, drawPos.Y + 2f, shadowColor);
                }

                // B. 绘制描边 (如果设置里有)
                // 对应 CanvasHelper.CreateFontEffect 里的 stroke 逻辑
                if (styleSettings.LyricsFontStrokeWidth > 0)
                {
                    // 注意：DrawTextLayout 不支持直接描边，需要用 DrawGeometry
                    // 如果这一步很卡，可以考虑去掉描边，或者只对当前行描边
                    if (line.OriginalCanvasGeometry != null)
                    {
                        ds.DrawGeometry(line.OriginalCanvasGeometry, drawPos, _strokeFontColor, styleSettings.LyricsFontStrokeWidth);
                    }
                }

                // C. 绘制底层文本 (底色)
                // 对应 CanvasHelper 里的 DrawTextLayout
                var baseColor = _strokeFontColor.WithAlpha((byte)(_strokeFontColor.A * _lyricsOpacityTransition.Value));
                ds.DrawTextLayout(textLayout, drawPos, baseColor);


                // D. 绘制高亮/卡拉OK效果 (核心优化点)
                if (line.HighlightOpacityTransition.Value > 0)
                {
                    GetLinePlayingProgress(i, out int charStartIndex, out int charLength, out double charProgress);

                    // 整行高亮或非逐字模式
                    if (charStartIndex >= line.OriginalText.Length || !isKaraokeEnabled)
                    {
                        var hlColor = _fgFontColor.WithAlpha((byte)(_fgFontColor.A * line.HighlightOpacityTransition.Value));
                        ds.DrawTextLayout(textLayout, drawPos, hlColor);
                    }
                    else
                    {
                        // === 计算裁剪区域 (照搬 CreateCharMask 的逻辑) ===
                        var regions = textLayout.GetCharacterRegions(charStartIndex, 1);

                        // 默认裁剪宽度覆盖到当前字之前
                        double validWidth = 0;
                        if (regions.Length > 0)
                        {
                            var region = regions[0];
                            // CanvasHelper 里的逻辑：region.LayoutBounds.X 是相对于 Layout 左上角的
                            // highlightWidth = TotalWidth * Progress
                            double highlightWidth = region.LayoutBounds.Width * charProgress;

                            // 当前高亮的总右边界 = 当前字左边 + 当前字播放过的宽度
                            validWidth = region.LayoutBounds.X + highlightWidth;
                        }
                        else if (charStartIndex > 0)
                        {
                            // 容错：如果取不到当前字区域（例如空格），取上一个字的右边缘
                            var prevRegions = textLayout.GetCharacterRegions(charStartIndex - 1, 1);
                            if (prevRegions.Length > 0) validWidth = prevRegions[0].LayoutBounds.Right;
                        }

                        if (validWidth > 0)
                        {
                            // === 创建 Layer 进行裁剪 ===
                            // 裁剪矩形的 X/Y 必须加上 drawPos (OriginalPosition)
                            // 因为 CreateLayer 是基于当前 Transform 的全局坐标
                            var clipRect = new Rect(
                                drawPos.X,              // 从文字绘制起点的 X 开始
                                drawPos.Y,              // 从文字绘制起点的 Y 开始
                                validWidth,             // 宽度
                                textLayout.LayoutBounds.Height // 高度
                            );

                            using (ds.CreateLayer(1.0f, clipRect))
                            {
                                var hlColor = _fgFontColor.WithAlpha((byte)(_fgFontColor.A * line.HighlightOpacityTransition.Value));
                                ds.DrawTextLayout(textLayout, drawPos, hlColor);
                            }
                        }
                    }
                }

                // 重置变换，准备画下一行
                ds.Transform = Matrix3x2.Identity;
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
