using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class Vector2Extensions
    {
        extension(Vector2 vector2)
        {
            public AppSize ToAppSize()
            {
                return new AppSize(vector2.X, vector2.Y);
            }
        }
    }
}
