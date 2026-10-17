using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Models;

public struct AmbientSceneConfiguration
{
    public AmbientBackgroundStyle Background { get; set; }
    public AmbientElement Ambient { get; set; }
    public AmbientOverlayEffect Overlay { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is AmbientSceneConfiguration other)
        {
            return Background == other.Background && Ambient == other.Ambient && Overlay == other.Overlay;
        }
        return false;
    }

    public override int GetHashCode() => (Background, Ambient, Overlay).GetHashCode();
    public static bool operator ==(AmbientSceneConfiguration left, AmbientSceneConfiguration right) => left.Equals(right);
    public static bool operator !=(AmbientSceneConfiguration left, AmbientSceneConfiguration right) => !(left == right);
}
