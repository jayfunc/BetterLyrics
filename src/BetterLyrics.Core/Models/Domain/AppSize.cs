namespace BetterLyrics.Core.Models.Domain;

public record AppSize(double Width, double Height)
{
    public static readonly AppSize Empty = new(0, 0);
}
