using System.Numerics;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.WinUI3.Renderer.SemanticEffectsRenderer;

public class SemanticParticle
{
    public string Emoji = string.Empty;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public float Scale;
    public float Rotation;
    public float RotationSpeed;
    public float Opacity;
    public ParticleBehavior Behavior;
    public float TimeOffset;
    public Windows.UI.Color Color;
}
