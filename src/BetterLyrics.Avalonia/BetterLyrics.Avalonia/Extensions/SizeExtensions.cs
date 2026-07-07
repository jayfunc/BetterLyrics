using Avalonia;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class SizeExtensions
{
    extension(Size size)
    {
        public AppSize  ToAppSize() => new(size.Width, size.Height);
    }
}