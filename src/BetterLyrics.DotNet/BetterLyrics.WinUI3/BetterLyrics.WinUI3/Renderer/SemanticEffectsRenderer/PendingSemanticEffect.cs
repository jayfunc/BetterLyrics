namespace BetterLyrics.WinUI3.Renderer.SemanticEffectsRenderer;

public class PendingSemanticEffect
{
    public double TriggerTimeMs { get; set; }
    public SemanticEffectRule Rule { get; set; } = null!;
    public bool IsTriggered { get; set; }
}
