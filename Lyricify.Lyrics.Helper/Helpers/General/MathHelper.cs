namespace Lyricify.Lyrics.Helpers.General
{
    public static class MathHelper
    {
        /// <summary>
        /// Returns the smaller of two 32-bit signed integers.
        /// </summary>
        /// <param name="val1">The first of two 32-bit signed integers to compare.</param>
        /// <param name="val2">The second of two 32-bit signed integers to compare.</param>
        /// <returns>Parameter val1 or val2, whichever is smaller.</returns>
        public static int? Min(int? val1, int? val2)
        {
            if (val1.HasValue && val2.HasValue) return Math.Min(val1.Value, val2.Value);
            if (val1.HasValue) return val1.Value;
            if (val2.HasValue) return val2.Value;
            return null;
        }

        /// <summary>
        /// Returns the larger of two 32-bit signed integers.
        /// </summary>
        /// <param name="val1">The first of two 32-bit signed integers to compare.</param>
        /// <param name="val2">The second of two 32-bit signed integers to compare.</param>
        /// <returns>Parameter val1 or val2, whichever is larger.</returns>
        public static int? Max(int? val1, int? val2)
        {
            if (val1.HasValue && val2.HasValue) return Math.Max(val1.Value, val2.Value);
            if (val1.HasValue) return val1.Value;
            if (val2.HasValue) return val2.Value;
            return null;
        }

        /// <summary>
        /// Returns x if x is greater than zero, otherwise zero will be returned
        /// </summary>
        public static int GreaterThanZero(int x)
        {
            if (x < 0) return 0;
            else return x;
        }

        /// <summary>
        /// Returns x if x is greater than zero, otherwise zero will be returned
        /// </summary>
        public static double GreaterThanZero(double x)
        {
            if (x < 0) return 0;
            else return x;
        }

        /// <summary>
        /// Returns x if x is greater than zero, otherwise zero will be returned
        /// </summary>
        public static float GreaterThanZero(float x)
        {
            if (x < 0) return 0;
            else return x;
        }

        /// <summary>
        /// Returns x if x is greater than a minimum value, otherwise the minimum value will be returned
        /// </summary>
        public static int GreaterThan(int x, int min)
        {
            if (x < min) return min;
            else return x;
        }

        /// <summary>
        /// Returns x if x is greater than a minimum value, otherwise the minimum value will be returned
        /// </summary>
        public static double GreaterThan(double x, double min)
        {
            if (x < min) return min;
            else return x;
        }

        /// <summary>
        /// Returns x if x is greater than a minimum value, otherwise the minimum value will be returned
        /// </summary>
        public static float GreaterThan(float x, float min)
        {
            if (x < min) return min;
            else return x;
        }

        /// <summary>
        /// Returns whether x is between a and b
        /// </summary>
        /// <param name="containEdge">Contain a and b or not</param>
        public static bool IsBetween(this int x, int a, int b, bool containEdge = true)
        {
            if (a > b) Swap(ref a, ref b);
            if (!containEdge)
            {
                if (x < b && x > a) return true;
                else return false;
            }
            if (x <= b && x >= a) return true;
            else return false;
        }

        /// <summary>
        /// Returns whether x is between a and b
        /// </summary>
        /// <param name="containEdge">Contain a and b or not</param>
        public static bool IsBetween(this double x, double a, double b, bool containEdge = true)
        {
            if (a > b) Swap(ref a, ref b);
            if (!containEdge)
            {
                if (x < b && x > a) return true;
                else return false;
            }
            if (x <= b && x >= a) return true;
            else return false;
        }

        /// <summary>
        /// Returns whether x is between a and b
        /// </summary>
        /// <param name="containEdge">Contain a and b or not</param>
        public static bool IsBetween(this float x, float a, float b, bool containEdge = true)
        {
            if (a > b) Swap(ref a, ref b);
            if (!containEdge)
            {
                if (x < b && x > a) return true;
                else return false;
            }
            if (x <= b && x >= a) return true;
            else return false;
        }

        public static void Swap(ref int a, ref int b)
        {
            (b, a) = (a, b);
        }

        public static void Swap(ref double a, ref double b)
        {
            (b, a) = (a, b);
        }

        public static void Swap(ref float a, ref float b)
        {
            (b, a) = (a, b);
        }
    }
}
