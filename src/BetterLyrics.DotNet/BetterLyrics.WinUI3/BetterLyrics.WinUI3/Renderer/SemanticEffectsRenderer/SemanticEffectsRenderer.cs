using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer.SemanticEffectsRenderer;

public partial class SemanticEffectsRenderer : EffectRendererBase, IDisposable
{
    private readonly List<SemanticParticle> _particles = new();
    private readonly Random _random = new();
    
    private RenderLyricsLine? _currentLine;
    private readonly List<Renderer.SemanticEffectsRenderer.PendingSemanticEffect> _pendingEffects = new();

    private readonly List<SemanticEffectRule> _rules = new()
    {
        // 1. Love / Heart
        new() { Keywords = new[] { "爱", "love", "心", "heart", "愛", "恋", "ai", "koi", "kokoro", "사랑", "sarang", "마음", "maeum", "amor", "corazón", "amour", "cœur", "liebe", "herz" }, Emoji = "❤", Count = 15, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 50, 50) },
        // 2. Broken / Pain
        new() { Keywords = new[] { "痛", "伤", "broken", "分手", "傷", "itami", "kizu", "wakare", "아픔", "apeum", "상처", "sangcheo", "이별", "dolor", "roto", "douleur", "brisé", "schmerz", "gebrochen" }, Emoji = "💔", Count = 10, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 200, 50, 50) },
        // 3. Rain / Tear
        new() { Keywords = new[] { "雨", "rain", "泪", "cry", "哭", "ame", "涙", "namida", "泣", "비", "bi", "눈물", "nunmul", "울음", "lluvia", "llorar", "lágrima", "pluie", "pleurer", "regen", "weinen" }, Emoji = "💧", Count = 25, Behavior = ParticleBehavior.RainDown, ParticleColor = Windows.UI.Color.FromArgb(255, 100, 200, 255) },
        // 4. Snow / Cold
        new() { Keywords = new[] { "雪", "snow", "冷", "cold", "yuki", "tsumetai", "寒", "samui", "눈", "nun", "추위", "chuwi", "차갑", "nieve", "frio", "frío", "neige", "froid", "schnee", "kalt" }, Emoji = "❄", Count = 20, Behavior = ParticleBehavior.RainDown, ParticleColor = Windows.UI.Color.FromArgb(255, 200, 240, 255) },
        // 5. Star / Night
        new() { Keywords = new[] { "星", "star", "夜空", "night sky", "夜", "night", "hoshi", "yoru", "空", "sora", "별", "byeol", "밤", "bam", "하늘", "haneul", "estrella", "noche", "étoile", "nuit", "stern", "nacht" }, Emoji = "✨", Count = 20, Behavior = ParticleBehavior.Swirl, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 255, 150) },
        // 6. Moon
        new() { Keywords = new[] { "月", "moon", "tsuki", "달", "dal", "luna", "lune", "mond" }, Emoji = "🌙", Count = 8, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 255, 150) },
        // 7. Sun / Light
        new() { Keywords = new[] { "阳", "sun", "晴", "光明", "光", "light", "太阳", "太陽", "taiyou", "hikari", "hare", "해", "태양", "taeyang", "빛", "bit", "sol", "luz", "soleil", "lumière", "sonne", "licht" }, Emoji = "☀", Count = 15, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 200, 50) },
        // 8. Cloud
        new() { Keywords = new[] { "云", "cloud", "雲", "kumo", "구름", "gureum", "nube", "nuage", "wolke" }, Emoji = "☁", Count = 12, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 220, 220, 220) },
        // 9. Wind / Breeze
        new() { Keywords = new[] { "风", "wind", "breeze", "吹", "風", "kaze", "fuku", "바람", "baram", "viento", "vent" }, Emoji = "🍃", Count = 18, Behavior = ParticleBehavior.Swirl, ParticleColor = Windows.UI.Color.FromArgb(255, 150, 255, 150) },
        // 10. Flower / Spring
        new() { Keywords = new[] { "花", "flower", "bloom", "春", "hana", "haru", "咲", "꽃", "kkot", "봄", "bom", "flor", "primavera", "fleur", "printemps", "blume", "frühling" }, Emoji = "🌸", Count = 20, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 150, 200) },
        // 11. Sea / Wave
        new() { Keywords = new[] { "海", "sea", "ocean", "浪", "wave", "umi", "波", "nami", "바다", "bada", "파도", "pado", "mar", "ola", "mer", "vague", "meer", "welle" }, Emoji = "🌊", Count = 15, Behavior = ParticleBehavior.Swirl, ParticleColor = Windows.UI.Color.FromArgb(255, 50, 150, 255) },
        // 12. Fire / Burn
        new() { Keywords = new[] { "火", "fire", "燃", "burn", "hi", "炎", "honoo", "moe", "불", "bul", "타오", "fuego", "quemar", "feu", "brûler", "feuer", "brennen" }, Emoji = "🔥", Count = 20, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 100, 0) },
        // 13. Dream / Magic
        new() { Keywords = new[] { "梦", "dream", "幻", "magic", "夢", "yume", "maboroshi", "魔法", "mahou", "꿈", "kkum", "마법", "mabeop", "sueño", "magia", "rêve", "magie", "traum" }, Emoji = "💭", Count = 15, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 200, 150, 255) },
        // 14. Fly / Bird
        new() { Keywords = new[] { "飞", "fly", "鸟", "bird", "翅膀", "wings", "飛", "tobi", "鳥", "tori", "羽", "hane", "翼", "tsubasa", "새", "sae", "날아", "nal-a", "날개", "volar", "pájaro", "voler", "oiseau", "fliegen", "vogel" }, Emoji = "🕊", Count = 12, Behavior = ParticleBehavior.Swirl, ParticleColor = Windows.UI.Color.FromArgb(255, 240, 240, 240) },
        // 15. Music / Song
        new() { Keywords = new[] { "歌", "music", "song", "旋律", "melody", "唱", "sing", "uta", "音楽", "ongaku", "メロディ", "merodi", "노래", "norae", "음악", "eumak", "멜로디", "música", "canción", "cantar", "musique", "chanson", "musik", "lied", "singen" }, Emoji = "🎵", Count = 20, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 100, 150) },
        // 16. Shine
        new() { Keywords = new[] { "闪", "耀", "shine", "亮", "輝", "kagaya", "光る", "hikaru", "빛나", "binna", "반짝", "brillar", "briller", "scheinen", "leuchten" }, Emoji = "🌟", Count = 15, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 255, 150) },
        // 17. Time
        new() { Keywords = new[] { "时间", "time", "岁月", "年华", "钟", "clock", "時間", "jikan", "時", "toki", "時計", "tokei", "시간", "sigan", "시계", "tiempo", "reloj", "temps", "horloge", "zeit", "uhr" }, Emoji = "⌛", Count = 10, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 200, 200, 200) },
        // 18. Smile / Joy
        new() { Keywords = new[] { "笑", "smile", "开心", "快乐", "happy", "joy", "warau", "えがお", "egao", "幸", "shiawase", "楽し", "tanoshi", "웃음", "useum", "행복", "haengbok", "기쁨", "sonreír", "feliz", "sourire", "heureux", "lächeln", "glücklich" }, Emoji = "😊", Count = 15, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 200, 0) },
        // 19. Kiss / Lip
        new() { Keywords = new[] { "吻", "kiss", "唇", "lip", "キス", "kisu", "kuchibiru", "くちづけ", "kuchidzuke", "키스", "kiseu", "입술", "ipsul", "beso", "labio", "bisou", "baiser", "kuss", "lippe" }, Emoji = "💋", Count = 12, Behavior = ParticleBehavior.FloatUp, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 50, 100) },
        // 20. Firework
        new() { Keywords = new[] { "烟花", "firework", "火花", "spark", "花火", "hanabi", "hibana", "불꽃", "bulkkot", "fuego artificial", "chispa", "feu d'artifice", "étincelle", "feuerwerk", "funke" }, Emoji = "🎆", Count = 25, Behavior = ParticleBehavior.Burst, ParticleColor = Windows.UI.Color.FromArgb(255, 255, 150, 50) }
    };

    private readonly Dictionary<string, CanvasTextLayout> _emojiCache = new();

    private CanvasTextLayout GetEmojiLayout(ICanvasResourceCreator resourceCreator, string emoji)
    {
        if (_emojiCache.TryGetValue(emoji, out var layout)) return layout;
        
        using var format = new CanvasTextFormat
        {
            FontFamily = "Segoe UI Emoji",
            FontSize = 32,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };

        layout = new CanvasTextLayout(resourceCreator, emoji, format, 0, 0);
        _emojiCache[emoji] = layout;
        return layout;
    }

    public void Dispose()
    {
        _particles.Clear();
        foreach (var layout in _emojiCache.Values) layout.Dispose();
        _emojiCache.Clear();
    }

    public void PrepareLine(RenderLyricsLine line)
    {
        if (_currentLine == line) return;
        _currentLine = line;
        _pendingEffects.Clear();
        
        if (line == null) return;
        
        var lowerText = line.PrimaryText.ToLower();
        
        foreach (var rule in _rules)
        {
            foreach (var keyword in rule.Keywords)
            {
                int index = lowerText.IndexOf(keyword);
                while (index != -1)
                {
                    var syllable = line.PrimaryRenderSyllables?.FirstOrDefault(s => s.StartIndex <= index && s.EndIndex >= index);
                    double triggerTimeMs = syllable != null ? syllable.StartMs : line.StartMs;
                    
                    triggerTimeMs -= 150; // trigger slightly before the word is sung for better visual rhythm
                    
                    _pendingEffects.Add(new PendingSemanticEffect 
                    {
                        TriggerTimeMs = triggerTimeMs,
                        Rule = rule,
                        IsTriggered = false
                    });
                    
                    index = lowerText.IndexOf(keyword, index + keyword.Length);
                }
            }
        }
    }

    public void TriggerEffect(string lyricsText, Vector2 canvasSize)
    {
    }

    private void SpawnParticles(Vector2 canvasSize, SemanticEffectRule rule)
    {
        var centerX = canvasSize.X / 2f;
        var centerY = canvasSize.Y / 2f;

        for (int i = 0; i < rule.Count; i++)
        {
            var p = new SemanticParticle
            {
                Emoji = rule.Emoji,
                Behavior = rule.Behavior,
                Life = 0,
                MaxLife = (float)(_random.NextDouble() * 2.5 + 1.5), // 1.5 to 4 seconds
                Scale = (float)(_random.NextDouble() * 1.5 + 0.8),
                Rotation = (float)(_random.NextDouble() * Math.PI * 2),
                RotationSpeed = (float)((_random.NextDouble() - 0.5) * 4), 
                Opacity = 1.0f,
                TimeOffset = (float)(_random.NextDouble() * Math.PI * 2),
                Color = rule.ParticleColor
            };

            switch (rule.Behavior)
            {
                case ParticleBehavior.RainDown:
                    p.Position = new Vector2(
                        (float)(_random.NextDouble() * canvasSize.X), 
                        (float)(_random.NextDouble() * -canvasSize.Y * 0.5f) - 50
                    );
                    p.Velocity = new Vector2((float)((_random.NextDouble() - 0.5) * 60), (float)(_random.NextDouble() * 150 + 200));
                    break;

                case ParticleBehavior.Burst:
                    p.Position = new Vector2(
                        centerX + (float)((_random.NextDouble() - 0.5) * 200),
                        centerY + (float)((_random.NextDouble() - 0.5) * 100)
                    );
                    var angle = _random.NextDouble() * Math.PI * 2;
                    var speed = _random.NextDouble() * 300 + 100;
                    p.Velocity = new Vector2((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed));
                    p.RotationSpeed = (float)((_random.NextDouble() - 0.5) * 8); // faster spin
                    break;

                case ParticleBehavior.Swirl:
                    p.Position = new Vector2(
                        (float)(_random.NextDouble() * canvasSize.X), 
                        canvasSize.Y + (float)(_random.NextDouble() * canvasSize.Y * 0.2f)
                    );
                    p.Velocity = new Vector2((float)((_random.NextDouble() - 0.5) * 100), (float)(_random.NextDouble() * -100 - 50));
                    p.MaxLife = (float)(_random.NextDouble() * 3 + 3);
                    break;

                case ParticleBehavior.FloatUp:
                default:
                    p.Position = new Vector2(
                        (float)(_random.NextDouble() * canvasSize.X), 
                        canvasSize.Y + (float)(_random.NextDouble() * canvasSize.Y * 0.3f) + 50
                    );
                    p.Velocity = new Vector2((float)((_random.NextDouble() - 0.5) * 120), (float)(_random.NextDouble() * -150 - 50));
                    break;
            }
            
            _particles.Add(p);
        }
    }

    public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity, bool is3DEnabled, double currentProgressMs)
    {
        float dt = (float)deltaTime.TotalSeconds;
        var canvasSize = new Vector2((float)control.Size.Width, (float)control.Size.Height);

        foreach (var effect in _pendingEffects)
        {
            if (!effect.IsTriggered && currentProgressMs >= effect.TriggerTimeMs)
            {
                effect.IsTriggered = true;
                SpawnParticles(canvasSize, effect.Rule);
            }
        }

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Life += dt;
            if (p.Life >= p.MaxLife)
            {
                _particles.RemoveAt(i);
                continue;
            }

            if (p.Behavior == ParticleBehavior.Swirl)
            {
                p.Velocity.X += (float)(Math.Sin(p.Life * 3 + p.TimeOffset) * 120 * dt);
                p.Velocity.Y -= 30 * dt;
            }
            else if (p.Behavior == ParticleBehavior.Burst)
            {
                p.Velocity *= (1.0f - 2.5f * dt);
                p.Velocity.Y += 80 * dt; // Gravity
            }

            p.Position += p.Velocity * dt;
            p.Rotation += p.RotationSpeed * dt;

            float fadeTime = 0.4f;
            if (p.Life < fadeTime)
            {
                p.Opacity = p.Life / fadeTime;
            }
            else if (p.Life > p.MaxLife - fadeTime)
            {
                p.Opacity = (p.MaxLife - p.Life) / fadeTime;
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

        var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);
        
        ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

        var oldTransform = ds.Transform;

        foreach (var p in _particles)
        {
            var transform = Matrix3x2.CreateRotation(p.Rotation, p.Position) *
                            Matrix3x2.CreateScale(p.Scale, p.Position);
            
            ds.Transform = transform * oldTransform; 

            var layout = GetEmojiLayout(control, p.Emoji);
            
            var c = p.Color;
            var finalColor = Windows.UI.Color.FromArgb((byte)Math.Clamp(p.Opacity * 255, 0, 255), c.R, c.G, c.B);
            
            ds.DrawTextLayout(layout, p.Position, finalColor);
        }

        ds.Transform = oldTransform;
        
        ResetTransform(ds, isBreathingEffectEnabled);
    }
}
