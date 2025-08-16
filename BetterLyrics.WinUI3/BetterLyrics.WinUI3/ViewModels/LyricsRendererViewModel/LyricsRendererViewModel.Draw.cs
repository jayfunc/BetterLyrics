using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            }

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            switch (_liveStatesService.LiveStates.CurrentLyricsWindowMode)
            {
                case LyricsWindowMode.DockMode:
                    FillBackground(control, combinedDs, _immersiveBgColorTransition.Value, 0f,
                        _immersiveBgOpacityTransition.Value * _settingsService.AppSettings.LyricsBackgroundSettings.PureColorOverlayOpacity / 100f);
                    break;
                case LyricsWindowMode.DesktopMode:
                    FillBackground(control, combinedDs, _immersiveBgColorTransition.Value, 0f,
                        _immersiveBgOpacityTransition.Value * _settingsService.AppSettings.LyricsBackgroundSettings.PureColorOverlayOpacity / 100f);
                    break;
                case LyricsWindowMode.StandardMode:
                case LyricsWindowMode.PictureInPictureMode:
                    FillBackground(control, combinedDs, _albumArtAccentColorTransition.Value, 0f, _settingsService.AppSettings.LyricsBackgroundSettings.PureColorOverlayOpacity / 100.0);
                    DrawAlbumArtBackground(control, combinedDs);
                    break;
                default:
                    break;
            }

            combinedDs.DrawImage(blurredLyrics);

            ds.DrawImage(combined);

            DrawAlbumArt(control, ds);
            DrawTitleAndArtist(control, ds);

            if (_isDebugOverlayEnabled)
            {
                _drawFrameCount++;

                var currentPlayingLine = _lyricsDataArr
                    .ElementAtOrDefault(_langIndex)
                    ?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

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
                            $"Cur time: {TotalTime + _positionOffset}\n" +
                            $"Lang size: {_lyricsDataArr.Count}\n" +
                            $"Song duration: {TimeSpan.FromMilliseconds(SongInfo?.DurationMs ?? 0)}\n" +
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
            ds.Transform = Matrix3x2.CreateRotation((float)_rotateAngle, control.Size.ToVector2() * 0.5f);

            if (_albumArtBgEffect != null)
            {
                ds.DrawImage(_albumArtBgEffect);
            }
            else if (_albumArtBgRenderTarget != null)
            {
                double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
                float offsetX = (float)(_canvasWidth - targetSize) / 2;
                float offsetY = (float)(_canvasHeight - targetSize) / 2;

                ds.DrawImage(_albumArtBgRenderTarget, new Vector2(offsetX, offsetY));
            }

            ds.Transform = Matrix3x2.Identity;
        }

        private void DrawAlbumArt(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_isAlbumArtEffectChanged && _albumArtEffect != null)
            {
                ds.DrawImage(new OpacityEffect
                {
                    Source = _albumArtEffect,
                    Opacity = (float)_albumArtOpacityTransition.Value
                }, new Vector2((float)_albumArtXTransition.Value, (float)_albumArtYTransition.Value));
            }
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

        private void DrawTitleAndArtist(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (_lastSongTitle != null || _lastSongArtist != null)
            {
                DrawSingleTitleAndArtist(control, ds, _lastSongTitle, _lastSongArtist, 1 - _songInfoOpacityTransition.Value);
            }
            if (_songTitle != null || _songArtist != null)
            {
                DrawSingleTitleAndArtist(control, ds, _songTitle, _songArtist, _songInfoOpacityTransition.Value);
            }
        }

        private void DrawSingleTitleAndArtist(ICanvasAnimatedControl control, CanvasDrawingSession ds, string? title, string? artist, double opacity)
        {
            var maxWidth = _lyricsLayoutOrientation switch
            {
                LyricsLayoutOrientation.Horizontal => _albumArtSize,
                LyricsLayoutOrientation.Vertical => _canvasWidth - _leftMargin - _albumArtSize - _rightMargin,
                _ => 0f
            };
            if (maxWidth <= 0)
            {
                return;
            }

            using CanvasTextLayout titleLayout = new(
                control, title ?? string.Empty,
                _titleTextFormat, (float)maxWidth, (float)_canvasHeight
            );
            using CanvasTextLayout artistLayout = new(
                control, artist ?? string.Empty,
                _artistTextFormat, (float)maxWidth, (float)_canvasHeight
            );
            ds.DrawTextLayout(
                titleLayout,
                new Vector2((float)_titleXTransition.Value, (float)_titleYTransition.Value),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 255 * opacity)));
            ds.DrawTextLayout(
                artistLayout,
                new Vector2((float)_titleXTransition.Value, (float)(_titleYTransition.Value + titleLayout.LayoutBounds.Height)),
                _bgFontColor.WithAlpha((byte)(_albumArtOpacityTransition.Value * 128 * opacity)));
        }

        private void DrawBlurredLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            var currentPlayingLine = _lyricsDataArr
                .ElementAtOrDefault(_langIndex)
                ?.LyricsLines.ElementAtOrDefault(_playingLineIndex);

            if (currentPlayingLine == null)
            {
                return;
            }

            for (int i = _startVisibleLineIndex; i <= _endVisibleLineIndex; i++)
            {
                var line = _lyricsDataArr.ElementAtOrDefault(_langIndex)?.LyricsLines.ElementAtOrDefault(i);
                if (line == null) continue;

                var textLayout = line.CanvasTextLayout;
                if (textLayout == null) continue;

                double layoutWidth = (double)textLayout.LayoutBounds.Width;
                double layoutHeight = (double)textLayout.LayoutBounds.Height;

                if (layoutWidth <= 0 || layoutHeight <= 0) continue;

                double yOffset = line.YOffsetTransition.Value + _canvasHeight / 2 + _lyricsYTransition.Value;

                // 组合变换：缩放 -> 旋转 -> 平移
                ds.Transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition)
                    * Matrix3x2.CreateRotation((float)line.AngleTransition.Value, currentPlayingLine.Position)
                    * Matrix3x2.CreateTranslation((float)_lyricsXTransition.Value, (float)yOffset);

                using var combined = new CanvasCommandList(control);
                using var combinedDs = combined.CreateDrawingSession();

                // Mock gradient blurred lyrics layer
                // 先铺一层带默认透明度的已经加了模糊效果的歌词作为最底层（背景歌词层次）
                // Current line will not be blurred
                using var backgroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor, 
                    _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFontStrokeWidth, _bgFontColor);
                using var backgroundEffect = CanvasHelper.CreateBackgroundEffect(line, backgroundFontEffect, _lyricsOpacityTransition.Value);
                combinedDs.DrawImage(backgroundEffect);

                //if (i == _playingLineIndex)
                if (line.HighlightOpacityTransition.Value != 0)
                {
                    GetLinePlayingProgress(i, out int charStartIndex, out int charLength, out double charProgress);

                    using var charMask = CanvasHelper.CreateCharMask(control, line, charStartIndex, charLength, charProgress);
                    using var lineStartToCharMask = CanvasHelper.CreateLineStartToCharMask(control, line, charStartIndex, charLength, charProgress,
                        _liveStatesService.LiveStates.CurrentLyricsEffectSettings.IsLyricsLineFadeEnabled);
                    using var lineMask = CanvasHelper.CreateLineMask(control, line);

                    using var foregroundFontEffect = CanvasHelper.CreateFontEffect(line, control, _strokeFontColor, 
                        _liveStatesService.LiveStates.CurrentLyricsStyleSettings.LyricsFontStrokeWidth, _bgFontColor);

                    using var effectLayer = new CanvasCommandList(control);
                    using var effectLayerDs = effectLayer.CreateDrawingSession();
                    if (_liveStatesService.LiveStates.CurrentLyricsEffectSettings.IsLyricsShadowEnabled)
                    {
                        var shadowEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask, 
                            _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsShadowScope);
                        using var foregroundShadowEffect = CanvasHelper.CreateForegroundShadowEffect(foregroundFontEffect, shadowEffectMask, 
                            _albumArtAccentColorTransition.Value, _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsShadowAmount);
                        effectLayerDs.DrawImage(foregroundShadowEffect);
                    }
                    if (_liveStatesService.LiveStates.CurrentLyricsEffectSettings.IsLyricsGlowEffectEnabled)
                    {
                        var blurEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                            _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsGlowEffectScope);
                        using var foregroundBlurEffect = CanvasHelper.CreateForegroundBlurEffect(foregroundFontEffect, blurEffectMask,
                            _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsGlowEffectAmount);
                        effectLayerDs.DrawImage(foregroundBlurEffect);
                    }
                    var highlightEffectMask = CanvasHelper.GetAlphaMask(control, charMask, lineStartToCharMask, lineMask,
                        _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsHighlightScope);
                    using var foregroundHighlightEffect = CanvasHelper.CreateForegroundHighlightEffect(foregroundFontEffect, highlightEffectMask);
                    effectLayerDs.DrawImage(foregroundHighlightEffect);

                    combinedDs.DrawImage(new OpacityEffect
                    {
                        Source = effectLayer,
                        Opacity = (float)(line.HighlightOpacityTransition.Value * _lyricsOpacityTransition.Value),
                    });

                    if (i == _playingLineIndex)
                    {
                        if (_liveStatesService.LiveStates.CurrentLyricsEffectSettings.IsLyricsFloatAnimationEnabled)
                        {
                            ds.DrawImage(new DisplacementMapEffect
                            {
                                Source = combined,
                                Displacement = lineStartToCharMask,
                                XChannelSelect = EffectChannelSelect.Red,
                                YChannelSelect = EffectChannelSelect.Alpha,
                                Amount = _liveStatesService.LiveStates.CurrentLyricsEffectSettings.LyricsFloatAmount,
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

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, Color color, double radius, double opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                (float)radius,
                (float)radius,
                color.WithAlpha((byte)(opacity * 255))
            );
        }

        private void FillBackground(ICanvasAnimatedControl control, CanvasDrawingSession ds, CanvasLinearGradientBrush brush, double radius, double opacity)
        {
            ds.FillRoundedRectangle(
                new Rect(0, 0, _canvasWidth, _canvasHeight),
                (float)radius,
                (float)radius,
                brush
            );
        }

        private CanvasLinearGradientBrush CreateVerticalFillBrush(
            ICanvasAnimatedControl control,
            List<(double position, Color color)> stops,
            double startY,
            double height
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(x => new CanvasGradientStop
            {
                Position = (float)x.position,
                Color = x.color,
            }).ToArray())
            {
                StartPoint = new Vector2(0, (float)startY),
                EndPoint = new Vector2(0, (float)(startY + height)),
            };
        }
    }
}
