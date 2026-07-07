namespace BetterLyrics.Core.Models.Domain;

public record AppPoint(double X, double Y)
{
    public static readonly AppPoint Empty = new(0, 0);
}
