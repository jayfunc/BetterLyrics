using System.Drawing;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.WinUI3.Extensions;

public static class RectangleExtensions
{
    extension(Rectangle rect)
    {
        public AppRect ToAppRect()
        {
            return new AppRect(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top
            );
        }
    }
}