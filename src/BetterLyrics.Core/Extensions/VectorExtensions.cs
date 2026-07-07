using System.Numerics;

namespace BetterLyrics.Core.Extensions;

public static class VectorExtensions
{
    extension(Vector2 vector2)
    {
        public Vector2 WithX(float x)
        {
            return new Vector2(x, vector2.Y);
        }

        public Vector2 WithY(float y)
        {
            return new Vector2(vector2.X, y);
        }

        public Vector2 AddX(float x)
        {
            return new Vector2(vector2.X + x, vector2.Y);
        }

        public Vector2 AddY(float y)
        {
            return new Vector2(vector2.X, vector2.Y + y);
        }
    }
}