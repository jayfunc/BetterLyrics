using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer;

public class SemanticParticle
{
    public string Emoji { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Life { get; set; }
    public float MaxLife { get; set; }
    public float Scale { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public float Opacity { get; set; }
}

public class SemanticEffectsRenderer : EffectRendererBase, IDisposable
{
    private readonly List<SemanticParticle> _particles = new();
    private readonly Random _random = new();

    public void Dispose()
    {
        _particles.Clear();
    }

    public void TriggerEffect(string lyricsText, Vector2 canvasSize)
    {
        if (string.IsNullOrWhiteSpace(lyricsText)) return;

        var lowerText = lyricsText.ToLower();

        // 爱情 / 心
        if (lowerText.Contains("爱") || lowerText.Contains("love") || lowerText.Contains("心") || lowerText.Contains("heart"))
        {
            SpawnParticles(canvasSize, "❤️", 15);
        }
        // 悲伤 / 破碎
        else if (lowerText.Contains("痛") || lowerText.Contains("伤") || lowerText.Contains("broken") || lowerText.Contains("分手"))
        {
            SpawnParticles(canvasSize, "💔", 10);
        }
        // 哭泣 / 眼泪 / 雨
        else if (lowerText.Contains("雨") || lowerText.Contains("rain") || lowerText.Contains("泪") || lowerText.Contains("cry") || lowerText.Contains("哭"))
        {
            SpawnParticles(canvasSize, "💧", 20, true);
        }
        // 雪 / 寒冷
        else if (lowerText.Contains("雪") || lowerText.Contains("snow") || lowerText.Contains("冷") || lowerText.Contains("cold"))
        {
            SpawnParticles(canvasSize, "❄️", 20, true);
        }
        // 星星 / 夜空
        else if (lowerText.Contains("星") || lowerText.Contains("star") || lowerText.Contains("夜空") || lowerText.Contains("night sky"))
        {
            SpawnParticles(canvasSize, "✨", 20);
        }
        // 月亮
        else if (lowerText.Contains("月") || lowerText.Contains("moon"))
        {
            SpawnParticles(canvasSize, "🌙", 8);
        }
        // 太阳 / 阳光
        else if (lowerText.Contains("阳") || lowerText.Contains("sun") || lowerText.Contains("晴") || lowerText.Contains("光明"))
        {
            SpawnParticles(canvasSize, "☀️", 10);
        }
        // 云
        else if (lowerText.Contains("云") || lowerText.Contains("cloud"))
        {
            SpawnParticles(canvasSize, "☁️", 12);
        }
        // 风 / 飘落
        else if (lowerText.Contains("风") || lowerText.Contains("wind") || lowerText.Contains("breeze"))
        {
            SpawnParticles(canvasSize, "🍃", 15, true);
        }
        // 花 / 春天
        else if (lowerText.Contains("花") || lowerText.Contains("flower") || lowerText.Contains("bloom") || lowerText.Contains("春"))
        {
            SpawnParticles(canvasSize, "🌸", 15, true);
        }
        // 大海 / 波浪
        else if (lowerText.Contains("海") || lowerText.Contains("sea") || lowerText.Contains("ocean") || lowerText.Contains("浪") || lowerText.Contains("wave"))
        {
            SpawnParticles(canvasSize, "🌊", 12);
        }
        // 火焰 / 燃烧
        else if (lowerText.Contains("火") || lowerText.Contains("fire") || lowerText.Contains("燃") || lowerText.Contains("burn"))
        {
            SpawnParticles(canvasSize, "🔥", 15);
        }
        // 梦 / 幻想
        else if (lowerText.Contains("梦") || lowerText.Contains("dream") || lowerText.Contains("幻"))
        {
            SpawnParticles(canvasSize, "💭", 15);
        }
        // 飞翔 / 鸟
        else if (lowerText.Contains("飞") || lowerText.Contains("fly") || lowerText.Contains("鸟") || lowerText.Contains("bird") || lowerText.Contains("翅膀") || lowerText.Contains("wings"))
        {
            SpawnParticles(canvasSize, "🕊️", 10);
        }
        // 音乐 / 歌
        else if (lowerText.Contains("歌") || lowerText.Contains("music") || lowerText.Contains("song") || lowerText.Contains("旋律") || lowerText.Contains("melody") || lowerText.Contains("唱") || lowerText.Contains("sing"))
        {
            SpawnParticles(canvasSize, "🎵", 15);
        }
        // 光芒 / 闪耀
        else if (lowerText.Contains("光") || lowerText.Contains("light") || lowerText.Contains("耀") || lowerText.Contains("shine"))
        {
            SpawnParticles(canvasSize, "🌟", 10);
        }
        // 时间 / 岁月
        else if (lowerText.Contains("时间") || lowerText.Contains("time") || lowerText.Contains("岁月") || lowerText.Contains("年华"))
        {
            SpawnParticles(canvasSize, "⏳", 8);
        }
    }

    private void SpawnParticles(Vector2 canvasSize, string emoji, int count, bool isFalling = false)
    {
        for (int i = 0; i < count; i++)
        {
            var p = new SemanticParticle
            {
                Emoji = emoji,
                Position = new Vector2(
                    (float)(_random.NextDouble() * canvasSize.X), 
                    isFalling ? (float)(_random.NextDouble() * -canvasSize.Y * 0.5f) : (float)(_random.NextDouble() * canvasSize.Y)
                ),
                Life = 0,
                MaxLife = (float)(_random.NextDouble() * 3 + 3), // 3 to 6 seconds
                Scale = (float)(_random.NextDouble() * 1.0 + 1.0),
                Rotation = (float)(_random.NextDouble() * Math.PI * 2),
                RotationSpeed = (float)((_random.NextDouble() - 0.5) * 2),
                Opacity = 1.0f
            };

            if (isFalling)
            {
                p.Velocity = new Vector2((float)((_random.NextDouble() - 0.5) * 50), (float)(_random.NextDouble() * 100 + 150));
            }
            else
            {
                p.Velocity = new Vector2((float)((_random.NextDouble() - 0.5) * 100), (float)(_random.NextDouble() * -80 - 40));
            }
            
            _particles.Add(p);
        }
    }

    public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity, bool is3DEnabled)
    {
        float dt = (float)deltaTime.TotalSeconds;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life += dt;
            if (p.Life >= p.MaxLife)
            {
                _particles.RemoveAt(i);
                continue;
            }

            p.Position += p.Velocity * dt;
            p.Rotation += p.RotationSpeed * dt;

            // Fade in/out
            if (p.Life < 0.5f)
            {
                p.Opacity = p.Life / 0.5f;
            }
            else if (p.Life > p.MaxLife - 1.0f)
            {
                p.Opacity = (p.MaxLife - p.Life);
            }
            else
            {
                p.Opacity = 1.0f;
            }
        }

        UpdateBreathing(bassEnergy, breathingIntensity);

        if (is3DEnabled)
        {
            var center = new Vector3((float)control.Size.Width / 2, (float)control.Size.Height / 2, 0);
            UpdateParallaxMatrix(center, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
    {
        if (_particles.Count == 0) return;

        using var format = new CanvasTextFormat
        {
            FontSize = 32,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };

        var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);
        
        // _threeDimMatrix is used by Transform3DEffect which is for images, 
        // since we are drawing text directly, we can't easily apply Transform3DEffect to text unless we draw to a command list.
        // For simplicity, we just use Matrix3x2 for 2D parallax if needed, or just let it float.
        // We will just draw the text with basic 2D transformations.
        
        ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

        var oldTransform = ds.Transform;

        foreach (var p in _particles)
        {
            var transform = Matrix3x2.CreateRotation(p.Rotation, p.Position) *
                            Matrix3x2.CreateScale(p.Scale, p.Position);
            
            ds.Transform = transform * oldTransform; 

            ds.DrawText(p.Emoji, p.Position, Windows.UI.Color.FromArgb((byte)(Math.Clamp(p.Opacity * 255, 0, 255)), 255, 255, 255), format);
        }

        ds.Transform = oldTransform;
        
        ResetTransform(ds, isBreathingEffectEnabled);
    }
}
