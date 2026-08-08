using System;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;

namespace BetterLyrics.WinUI3.Controls;

public enum BackgroundStyle
{
    WarmDark,
    DeepGreen,
    DeepBlue,
    DeepSpace,
    Gloomy,
    PitchBlack,
    GoldBlack,
    NeonPurple,
    SunsetOrange,
    FrostBlue
}

public enum AmbientElement
{
    None,
    GodRays,
    BlurredBlobs,
    SweepingHighlight,
    StageSpotlights,
    TrainPole,
    NeonGrid,
    NorthernLights
}

public enum OverlayEffect
{
    None,
    DustParticles,
    TrainStreaks,
    Stars,
    RainDrops,
    VinylRings,
    Snowflakes,
    FloatingEmbers,
    DataParticles
}

public struct SceneConfiguration
{
    public BackgroundStyle Background { get; set; }
    public AmbientElement Ambient { get; set; }
    public OverlayEffect Overlay { get; set; }
    
    public override bool Equals(object obj)
    {
        if (obj is SceneConfiguration other)
            return Background == other.Background && Ambient == other.Ambient && Overlay == other.Overlay;
        return false;
    }
    
    public override int GetHashCode() => (Background, Ambient, Overlay).GetHashCode();
    public static bool operator ==(SceneConfiguration left, SceneConfiguration right) => left.Equals(right);
    public static bool operator !=(SceneConfiguration left, SceneConfiguration right) => !(left == right);
}

public sealed partial class AmbientBackgroundControl : UserControl
{
    private SceneConfiguration _currentScene = new SceneConfiguration { Background = BackgroundStyle.WarmDark, Ambient = AmbientElement.GodRays, Overlay = OverlayEffect.DustParticles };
    private SceneConfiguration _previousScene = new SceneConfiguration { Background = BackgroundStyle.WarmDark, Ambient = AmbientElement.GodRays, Overlay = OverlayEffect.DustParticles };
    private float _transitionAlpha = 1.0f; 
    
    private float _time = 0;
    private Random _rnd = new Random();

    // Dust
    private struct DustParticle { public Vector2 Pos; public float Size; public float SpeedX; public float SpeedY; public float AlphaOffset; }
    private DustParticle[] _dustParticles;

    // BlurredBlobs
    private GaussianBlurEffect _treeBlur;

    // Train Streaks & Pole
    private struct TrainStreak { public float Y; public float Speed; public float Length; public float X; public float Alpha; }
    private TrainStreak[] _streaks;
    private float _poleX = 0;

    // Stars
    private struct Star { public Vector2 Pos; public float Size; public float BlinkRate; public float Phase; }
    private Star[] _stars;
    private float _shootingStarX = -100;
    private float _shootingStarY = -100;
    private float _shootingStarTime = 0;

    // Rain Drops
    private struct RainDrop { public Vector2 Pos; public float Speed; public float Size; }
    private RainDrop[] _rainDrops;
    private RainDrop[] _windowDrops;

    // Snowflakes
    private struct Snowflake { public Vector2 Pos; public float Speed; public float Size; public float WobbleOffset; }
    private Snowflake[] _snowflakes;

    // Floating Embers
    private struct Ember { public Vector2 Pos; public float Size; public float SpeedY; public float SpeedX; public float Life; }
    private Ember[] _embers;

    // Data Particles
    private struct DataParticle { public Vector2 Pos; public float Speed; public int Value; }
    private DataParticle[] _dataParticles;

    public AmbientBackgroundControl()
    {
        this.InitializeComponent();
        InitializeParticles();
    }
    
    private void InitializeParticles()
    {
        _dustParticles = new DustParticle[80];
        for(int i=0; i<80; i++)
        {
            _dustParticles[i] = new DustParticle {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Size = 1f + (float)_rnd.NextDouble() * 3f,
                SpeedX = ((float)_rnd.NextDouble() - 0.5f) * 0.05f,
                SpeedY = ((float)_rnd.NextDouble() - 0.5f) * 0.05f - 0.02f,
                AlphaOffset = (float)_rnd.NextDouble() * MathF.PI * 2
            };
        }

        _streaks = new TrainStreak[50];
        for(int i=0; i<50; i++)
        {
            _streaks[i] = new TrainStreak {
                Y = (float)_rnd.NextDouble(),
                X = (float)_rnd.NextDouble(),
                Speed = 0.5f + (float)_rnd.NextDouble() * 2.0f,
                Length = 0.05f + (float)_rnd.NextDouble() * 0.2f,
                Alpha = 0.1f + (float)_rnd.NextDouble() * 0.4f
            };
        }

        _stars = new Star[150];
        for(int i=0; i<150; i++)
        {
            _stars[i] = new Star {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Size = 0.5f + (float)_rnd.NextDouble() * 1.5f,
                BlinkRate = 0.5f + (float)_rnd.NextDouble() * 2f,
                Phase = (float)_rnd.NextDouble() * MathF.PI * 2
            };
        }

        _rainDrops = new RainDrop[100];
        for(int i=0; i<100; i++)
        {
            _rainDrops[i] = new RainDrop {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Speed = 1.0f + (float)_rnd.NextDouble() * 1.5f,
                Size = 1f
            };
        }
        _windowDrops = new RainDrop[30];
        for(int i=0; i<30; i++)
        {
            _windowDrops[i] = new RainDrop {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Speed = 0.05f + (float)_rnd.NextDouble() * 0.1f,
                Size = 2f + (float)_rnd.NextDouble() * 4f
            };
        }

        _snowflakes = new Snowflake[100];
        for(int i=0; i<100; i++)
        {
            _snowflakes[i] = new Snowflake {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Speed = 0.2f + (float)_rnd.NextDouble() * 0.5f,
                Size = 1f + (float)_rnd.NextDouble() * 2f,
                WobbleOffset = (float)_rnd.NextDouble() * MathF.PI * 2
            };
        }

        _embers = new Ember[60];
        for(int i=0; i<60; i++)
        {
            _embers[i] = new Ember {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Size = 1f + (float)_rnd.NextDouble() * 3f,
                SpeedY = 0.3f + (float)_rnd.NextDouble() * 0.6f,
                SpeedX = ((float)_rnd.NextDouble() - 0.5f) * 0.2f,
                Life = (float)_rnd.NextDouble()
            };
        }

        _dataParticles = new DataParticle[50];
        for(int i=0; i<50; i++)
        {
            _dataParticles[i] = new DataParticle {
                Pos = new Vector2((float)_rnd.NextDouble(), (float)_rnd.NextDouble()),
                Speed = 1.0f + (float)_rnd.NextDouble() * 2.0f,
                Value = _rnd.Next(2)
            };
        }
    }

    public void SetScene(SceneConfiguration newScene)
    {
        if (_currentScene == newScene) return;
        _previousScene = _currentScene;
        _currentScene = newScene;
        _transitionAlpha = 0f;
    }

    private void AnimatedCanvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
    {
        _treeBlur = new GaussianBlurEffect { BlurAmount = 80f, Optimization = EffectOptimization.Speed };
    }

    private void AnimatedCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
        float dt = (float)args.Timing.ElapsedTime.TotalSeconds;
        _time += dt;

        if (_transitionAlpha < 1.0f)
        {
            _transitionAlpha += dt * 1.5f; 
            if (_transitionAlpha > 1.0f) _transitionAlpha = 1.0f;
        }

        UpdateDust(dt);
        UpdateTrainStreaksAndPole(dt);
        UpdateStars(dt);
        UpdateRain(dt);
        UpdateSnowflakes(dt);
        UpdateEmbers(dt);
        UpdateDataParticles(dt);
    }

    private void AnimatedCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        float w = (float)sender.Size.Width;
        float h = (float)sender.Size.Height;
        
        if (w == 0 || h == 0) return;

        if (_transitionAlpha >= 1.0f)
        {
            DrawScene(_currentScene, sender, ds, w, h);
        }
        else
        {
            using (var clPrev = new CanvasCommandList(sender))
            using (var clCurr = new CanvasCommandList(sender))
            {
                using (var dsPrev = clPrev.CreateDrawingSession())
                {
                    DrawScene(_previousScene, sender, dsPrev, w, h);
                }
                using (var dsCurr = clCurr.CreateDrawingSession())
                {
                    DrawScene(_currentScene, sender, dsCurr, w, h);
                }

                ds.DrawImage(clPrev, 0, 0, new Windows.Foundation.Rect(0,0,w,h), 1.0f - _transitionAlpha);
                ds.DrawImage(clCurr, 0, 0, new Windows.Foundation.Rect(0,0,w,h), _transitionAlpha);
            }
        }
    }

    private void DrawScene(SceneConfiguration config, ICanvasAnimatedControl sender, CanvasDrawingSession ds, float w, float h)
    {
        DrawBackground(config.Background, sender, ds, w, h);
        DrawAmbient(config.Ambient, sender, ds, w, h);
        DrawOverlay(config.Overlay, sender, ds, w, h);
    }

    private void DrawBackground(BackgroundStyle style, ICanvasAnimatedControl rc, CanvasDrawingSession ds, float w, float h)
    {
        switch (style)
        {
            case BackgroundStyle.WarmDark:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 35, 15, 5));
                break;
            case BackgroundStyle.DeepGreen:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 10, 25, 15));
                break;
            case BackgroundStyle.DeepBlue:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 5, 10, 20));
                break;
            case BackgroundStyle.DeepSpace:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 2, 5, 15));
                break;
            case BackgroundStyle.Gloomy:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 15, 20, 25));
                break;
            case BackgroundStyle.PitchBlack:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 5, 5, 5));
                break;
            case BackgroundStyle.GoldBlack:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 10, 10, 10));
                break;
            case BackgroundStyle.NeonPurple:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 20, 5, 30));
                break;
            case BackgroundStyle.SunsetOrange:
                var sunsetStops = new[] { new CanvasGradientStop { Position = 0, Color = Color.FromArgb(255, 20, 10, 40) }, new CanvasGradientStop { Position = 1, Color = Color.FromArgb(255, 120, 30, 10) } };
                using (var brush = new CanvasLinearGradientBrush(rc, sunsetStops))
                {
                    brush.StartPoint = new Vector2(0, 0); brush.EndPoint = new Vector2(0, h);
                    ds.FillRectangle(0, 0, w, h, brush);
                }
                break;
            case BackgroundStyle.FrostBlue:
                ds.FillRectangle(0, 0, w, h, Color.FromArgb(255, 10, 15, 25));
                break;
        }
    }

    private void DrawAmbient(AmbientElement ambient, ICanvasAnimatedControl rc, CanvasDrawingSession ds, float w, float h)
    {
        switch (ambient)
        {
            case AmbientElement.GodRays:
                var stops = new[]
                {
                    new CanvasGradientStop { Position = 0.0f, Color = Color.FromArgb(120, 255, 200, 100) },
                    new CanvasGradientStop { Position = 0.4f, Color = Color.FromArgb(40, 255, 100, 0) },
                    new CanvasGradientStop { Position = 1.0f, Color = Color.FromArgb(0, 0, 0, 0) }
                };
                using (var brush = new CanvasLinearGradientBrush(rc, stops))
                {
                    brush.StartPoint = new Vector2(w * 0.8f, -h * 0.2f);
                    brush.EndPoint = new Vector2(-w * 0.2f, h * 1.2f);
                    ds.FillRectangle(0, 0, w, h, brush);
                }

                Vector2[] rayPoints = {
                    new Vector2(w * 0.4f, 0), new Vector2(w * 0.8f, 0),
                    new Vector2(w * 0.2f, h), new Vector2(-w * 0.2f, h)
                };
                ds.FillGeometry(CanvasGeometry.CreatePolygon(rc, rayPoints), Color.FromArgb(15, 255, 220, 150));
                break;
                
            case AmbientElement.BlurredBlobs:
                using (var cl = new CanvasCommandList(rc))
                {
                    using (var clds = cl.CreateDrawingSession())
                    {
                        float blobCount = 5;
                        for (int i = 0; i < blobCount; i++)
                        {
                            float bx = w * 0.5f + (float)Math.Sin(_time * 0.2f + i) * w * 0.4f;
                            float by = h * 0.5f + (float)Math.Cos(_time * 0.15f + i * 2) * h * 0.4f;
                            float br = w * 0.3f + (float)Math.Sin(_time * 0.3f + i) * w * 0.1f;
                            clds.FillCircle(bx, by, br, Color.FromArgb(100, 40, 90, 50));
                        }
                    }
                    _treeBlur.Source = cl;
                    ds.DrawImage(_treeBlur);
                }
                break;
                
            case AmbientElement.StageSpotlights:
                float swing1 = MathF.Sin(_time * 0.5f) * w * 0.3f;
                float swing2 = MathF.Cos(_time * 0.4f) * w * 0.3f;

                Vector2[] cone1 = { new Vector2(w * 0.2f, -50), new Vector2(w * 0.5f + swing1 - w * 0.3f, h), new Vector2(w * 0.5f + swing1 + w * 0.3f, h) };
                Vector2[] cone2 = { new Vector2(w * 0.8f, -50), new Vector2(w * 0.5f + swing2 - w * 0.3f, h), new Vector2(w * 0.5f + swing2 + w * 0.3f, h) };

                var coneStops = new[] { new CanvasGradientStop { Position = 0, Color = Color.FromArgb(80, 255, 255, 255) }, new CanvasGradientStop { Position = 1, Color = Color.FromArgb(0, 255, 255, 255) } };
                using (var brush = new CanvasLinearGradientBrush(rc, coneStops))
                {
                    brush.StartPoint = new Vector2(0, 0);
                    brush.EndPoint = new Vector2(0, h);
                    ds.FillGeometry(CanvasGeometry.CreatePolygon(rc, cone1), brush);
                    ds.FillGeometry(CanvasGeometry.CreatePolygon(rc, cone2), brush);
                }
                break;
                
            case AmbientElement.TrainPole:
                ds.FillRectangle(_poleX * w, 0, w * 0.1f, h, Color.FromArgb(120, 0, 0, 0));
                break;
                
            case AmbientElement.SweepingHighlight:
                float cx = w * 0.5f;
                float cy = h * 0.5f;
                float maxR = MathF.Max(w, h) * 0.8f;
                float angle = _time * 0.2f;
                float hx = MathF.Cos(angle) * maxR;
                float hy = MathF.Sin(angle) * maxR;

                var sweepStops = new[] {
                    new CanvasGradientStop { Position = 0, Color = Color.FromArgb(0, 255, 215, 0) },
                    new CanvasGradientStop { Position = 0.5f, Color = Color.FromArgb(60, 255, 215, 0) },
                    new CanvasGradientStop { Position = 1, Color = Color.FromArgb(0, 255, 215, 0) }
                };
                using (var brush = new CanvasLinearGradientBrush(rc, sweepStops))
                {
                    brush.StartPoint = new Vector2(cx - hx, cy - hy);
                    brush.EndPoint = new Vector2(cx + hx, cy + hy);
                    ds.FillCircle(cx, cy, maxR, brush);
                }
                break;
                
            case AmbientElement.NeonGrid:
                for(int i=0; i<10; i++)
                {
                    float y = h * 0.5f + MathF.Pow(i / 10f, 2) * h * 0.5f;
                    ds.DrawLine(0, y, w, y, Color.FromArgb(50, 255, 0, 255), 1f);
                }
                for(int i=-10; i<=10; i++)
                {
                    float xBot = w * 0.5f + i * w * 0.15f;
                    ds.DrawLine(w * 0.5f, h * 0.5f, xBot, h, Color.FromArgb(50, 0, 255, 255), 1f);
                }
                ds.FillRectangle(0, 0, w, h * 0.5f, Color.FromArgb(255, 20, 5, 30)); // Mask out above horizon
                break;

            case AmbientElement.NorthernLights:
                var nStops = new[] {
                    new CanvasGradientStop { Position = 0, Color = Color.FromArgb(0, 50, 255, 150) },
                    new CanvasGradientStop { Position = 0.5f, Color = Color.FromArgb(50, 50, 255, 150) },
                    new CanvasGradientStop { Position = 1, Color = Color.FromArgb(0, 50, 255, 150) }
                };
                using (var brush = new CanvasLinearGradientBrush(rc, nStops))
                {
                    brush.StartPoint = new Vector2(0, 0); brush.EndPoint = new Vector2(0, h * 0.6f);
                    float sway = MathF.Sin(_time * 0.5f) * w * 0.2f;
                    Vector2[] points = { new Vector2(w * 0.2f + sway, 0), new Vector2(w * 0.8f + sway, 0), new Vector2(w * 0.6f - sway, h * 0.6f), new Vector2(w * 0.4f - sway, h * 0.6f) };
                    ds.FillGeometry(CanvasGeometry.CreatePolygon(rc, points), brush);
                }
                break;

            case AmbientElement.None:
            default:
                break;
        }
    }

    private void DrawOverlay(OverlayEffect overlay, ICanvasAnimatedControl rc, CanvasDrawingSession ds, float w, float h)
    {
        switch (overlay)
        {
            case OverlayEffect.DustParticles:
                foreach (var p in _dustParticles)
                {
                    float alpha = 0.2f + 0.3f * (MathF.Sin(_time * 2f + p.AlphaOffset) * 0.5f + 0.5f);
                    ds.FillCircle(p.Pos.X * w, p.Pos.Y * h, p.Size, Color.FromArgb((byte)(alpha * 255), 255, 220, 180));
                }
                break;
                
            case OverlayEffect.TrainStreaks:
                foreach (var s in _streaks)
                {
                    ds.DrawLine(new Vector2(s.X * w, s.Y * h), new Vector2((s.X + s.Length) * w, s.Y * h), Color.FromArgb((byte)(s.Alpha * 255), 100, 150, 255), 2f);
                }
                break;
                
            case OverlayEffect.Stars:
                foreach (var s in _stars)
                {
                    float alpha = 0.3f + 0.7f * (MathF.Sin(_time * s.BlinkRate + s.Phase) * 0.5f + 0.5f);
                    ds.FillCircle(s.Pos.X * w, s.Pos.Y * h, s.Size, Color.FromArgb((byte)(alpha * 255), 200, 220, 255));
                }
                if (_shootingStarX > -0.5f)
                {
                    ds.DrawLine(new Vector2(_shootingStarX * w, _shootingStarY * h), new Vector2((_shootingStarX + 0.2f) * w, (_shootingStarY - 0.1f) * h), Color.FromArgb(200, 255, 255, 255), 2f);
                }
                break;
                
            case OverlayEffect.RainDrops:
                foreach (var r in _rainDrops)
                {
                    ds.DrawLine(new Vector2(r.Pos.X * w, r.Pos.Y * h), new Vector2((r.Pos.X - 0.02f) * w, (r.Pos.Y + 0.1f) * h), Color.FromArgb(60, 150, 180, 200), r.Size);
                }
                foreach (var wDrop in _windowDrops)
                {
                    ds.FillCircle(wDrop.Pos.X * w, wDrop.Pos.Y * h, wDrop.Size, Color.FromArgb(80, 200, 220, 255));
                }
                break;
                
            case OverlayEffect.VinylRings:
                float cx = w * 0.5f;
                float cy = h * 0.5f;
                float maxR = MathF.Max(w, h) * 0.8f;
                for (float r = 50; r < maxR; r += 15)
                {
                    ds.DrawCircle(cx, cy, r, Color.FromArgb(30, 255, 215, 0), 1f);
                }
                break;
                
            case OverlayEffect.Snowflakes:
                foreach (var s in _snowflakes)
                {
                    ds.FillCircle(s.Pos.X * w, s.Pos.Y * h, s.Size, Color.FromArgb(150, 255, 255, 255));
                }
                break;
                
            case OverlayEffect.FloatingEmbers:
                foreach (var e in _embers)
                {
                    float alpha = MathF.Max(0, 1.0f - e.Life);
                    ds.FillCircle(e.Pos.X * w, e.Pos.Y * h, e.Size, Color.FromArgb((byte)(alpha * 255), 255, 100, 0));
                }
                break;
                
            case OverlayEffect.DataParticles:
                foreach (var d in _dataParticles)
                {
                    ds.DrawText(d.Value.ToString(), d.Pos.X * w, d.Pos.Y * h, Color.FromArgb(150, 0, 255, 100), new Microsoft.Graphics.Canvas.Text.CanvasTextFormat { FontSize = 12 });
                }
                break;

            case OverlayEffect.None:
            default:
                break;
        }
    }

    private void UpdateDust(float dt)
    {
        for (int i = 0; i < _dustParticles.Length; i++)
        {
            _dustParticles[i].Pos.X += _dustParticles[i].SpeedX * dt;
            _dustParticles[i].Pos.Y += _dustParticles[i].SpeedY * dt;
            if (_dustParticles[i].Pos.Y < -0.1f) _dustParticles[i].Pos.Y = 1.1f;
            if (_dustParticles[i].Pos.Y > 1.1f) _dustParticles[i].Pos.Y = -0.1f;
            if (_dustParticles[i].Pos.X < -0.1f) _dustParticles[i].Pos.X = 1.1f;
            if (_dustParticles[i].Pos.X > 1.1f) _dustParticles[i].Pos.X = -0.1f;
        }
    }

    private void UpdateTrainStreaksAndPole(float dt)
    {
        for (int i = 0; i < _streaks.Length; i++)
        {
            _streaks[i].X -= _streaks[i].Speed * dt;
            if (_streaks[i].X + _streaks[i].Length < 0)
            {
                _streaks[i].X = 1.0f + (float)_rnd.NextDouble() * 0.5f;
                _streaks[i].Y = (float)_rnd.NextDouble();
            }
        }
        
        _poleX -= 1.5f * dt; 
        if (_poleX < -0.5f) _poleX = 1.0f + (float)_rnd.NextDouble() * 2f;
    }

    private void UpdateStars(float dt)
    {
        _shootingStarTime += dt;
        if (_shootingStarTime > 5.0f && _rnd.NextDouble() < 0.01)
        {
            _shootingStarX = 1.2f;
            _shootingStarY = -0.2f + (float)_rnd.NextDouble() * 0.5f;
            _shootingStarTime = 0;
        }

        if (_shootingStarX > -0.5f)
        {
            _shootingStarX -= 2.0f * dt;
            _shootingStarY += 1.0f * dt;
        }
    }

    private void UpdateRain(float dt)
    {
        for (int i = 0; i < _rainDrops.Length; i++)
        {
            _rainDrops[i].Pos.Y += _rainDrops[i].Speed * dt;
            _rainDrops[i].Pos.X -= _rainDrops[i].Speed * 0.2f * dt;
            if (_rainDrops[i].Pos.Y > 1.2f) { _rainDrops[i].Pos.Y = -0.2f; _rainDrops[i].Pos.X = (float)_rnd.NextDouble() * 1.2f; }
        }
        for (int i = 0; i < _windowDrops.Length; i++)
        {
            _windowDrops[i].Pos.Y += _windowDrops[i].Speed * dt;
            if (_windowDrops[i].Pos.Y > 1.2f) _windowDrops[i].Pos.Y = -0.2f;
        }
    }

    private void UpdateSnowflakes(float dt)
    {
        for (int i = 0; i < _snowflakes.Length; i++)
        {
            _snowflakes[i].Pos.Y += _snowflakes[i].Speed * dt;
            _snowflakes[i].Pos.X += MathF.Sin(_time + _snowflakes[i].WobbleOffset) * 0.05f * dt;
            if (_snowflakes[i].Pos.Y > 1.1f) { _snowflakes[i].Pos.Y = -0.1f; _snowflakes[i].Pos.X = (float)_rnd.NextDouble(); }
        }
    }

    private void UpdateEmbers(float dt)
    {
        for (int i = 0; i < _embers.Length; i++)
        {
            _embers[i].Pos.Y -= _embers[i].SpeedY * dt;
            _embers[i].Pos.X += _embers[i].SpeedX * dt;
            _embers[i].Life += dt * 0.5f;
            if (_embers[i].Pos.Y < -0.1f || _embers[i].Life > 1.0f) 
            { 
                _embers[i].Pos.Y = 1.1f; 
                _embers[i].Pos.X = (float)_rnd.NextDouble(); 
                _embers[i].Life = 0;
            }
        }
    }

    private void UpdateDataParticles(float dt)
    {
        for (int i = 0; i < _dataParticles.Length; i++)
        {
            _dataParticles[i].Pos.Y += _dataParticles[i].Speed * dt;
            if (_rnd.NextDouble() < 0.05) _dataParticles[i].Value = _rnd.Next(2);
            if (_dataParticles[i].Pos.Y > 1.1f) { _dataParticles[i].Pos.Y = -0.1f; _dataParticles[i].Pos.X = (float)_rnd.NextDouble(); }
        }
    }

    private void AnimatedCanvas_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        AnimatedCanvas.RemoveFromVisualTree();
        AnimatedCanvas = null;
    }
}
