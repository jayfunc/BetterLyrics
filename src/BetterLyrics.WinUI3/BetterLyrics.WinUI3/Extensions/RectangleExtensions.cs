using BetterLyrics.Core.Models.Domain;
using System.Drawing;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class RectangleExtensions
    {
        extension(Rectangle rect)
        {
            public AppRect ToAppRect() => new(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top
            );
        }
    }
}
