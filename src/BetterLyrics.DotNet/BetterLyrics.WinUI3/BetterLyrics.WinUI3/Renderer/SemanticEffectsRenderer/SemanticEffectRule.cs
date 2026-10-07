using System;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.WinUI3.Renderer.SemanticEffectsRenderer;

public class SemanticEffectRule
{
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public ParticleBehavior Behavior { get; set; }
    public Windows.UI.Color ParticleColor { get; set; } = Windows.UI.Color.FromArgb(255, 255, 255, 255);
}
