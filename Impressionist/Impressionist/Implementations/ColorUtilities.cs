using Impressionist.Abstractions;
using System;
using System.Numerics;

namespace Impressionist.Implementations
{
    public static class ColorUtilities
    {
        public static HSVColor RGBVectorToHSVColor(this Vector3 color)
        {
            HSVColor hsv = new HSVColor();

            float max = Math.Max(Math.Max(color.X, color.Y), color.Z);
            float min = Math.Min(Math.Min(color.X, color.Y), color.Z);

            hsv.V = max * 100 / 255;

            if (max == min)
            {
                hsv.H = 0;
                hsv.S = 0;
            }
            else
            {
                hsv.S = (((max - min) / max) * 100);

                hsv.H = 0;

                if (max == color.X)
                {
                    hsv.H = (60 * (color.Y - color.Z) / (max - min));
                    if (hsv.H < 0) hsv.H += 360;
                }
                else if (max == color.Y)
                {
                    hsv.H = (60 * (2 + (color.Z - color.X) / (max - min)));
                    if (hsv.H < 0) hsv.H += 360;
                }
                else if (max == color.Z)
                {
                    hsv.H = (60 * (4 + (color.X - color.Y) / (max - min)));
                    if (hsv.H < 0) hsv.H += 360;
                }

            }
            return hsv;
        }
        public static Vector3 HSVColorToRGBVector(this HSVColor hsv)
        {
            if (hsv.H == 360) hsv.H = 0;
            int Hi = (int)Math.Floor((float)hsv.H / 60) % 6;

            float f = (hsv.H / 60) - Hi;
            float p = (hsv.V / 100) * (1 - (hsv.S / 100));
            float q = (hsv.V / 100) * (1 - f * (hsv.S / 100));
            float t = (hsv.V / 100) * (1 - (1 - f) * (hsv.S / 100));

            p *= 255;
            q *= 255;
            t *= 255;

            Vector3 rgb = Vector3.Zero;

            switch (Hi)
            {
                case 0:
                    rgb = new Vector3(hsv.V * 255 / 100, t, p);
                    break;
                case 1:
                    rgb = new Vector3(q, hsv.V * 255 / 100, p);
                    break;
                case 2:
                    rgb = new Vector3(p, hsv.V * 255 / 100, t);
                    break;
                case 3:
                    rgb = new Vector3(p, q, hsv.V * 255 / 100);
                    break;
                case 4:
                    rgb = new Vector3(t, p, hsv.V * 255 / 100);
                    break;
                case 5:
                    rgb = new Vector3(hsv.V * 255 / 100, p, q);
                    break;
            }

            return rgb;
        }
        public static Vector3 RGBVectorToXYZVector(this Vector3 rgb)
        {
            var red = rgb.X;
            var green = rgb.Y;
            var blue = rgb.Z;
            // normalize red, green, blue values
            float rLinear = red / 255.0f;
            float gLinear = green / 255.0f;
            float bLinear = blue / 255.0f;

            // convert to a sRGB form
            float r = (rLinear > 0.04045) ? (float)Math.Pow((rLinear + 0.055) / (1 + 0.055), 2.2) : (float)(rLinear / 12.92);
            float g = (gLinear > 0.04045) ? (float)Math.Pow((gLinear + 0.055) / (1 + 0.055), 2.2) : (float)(gLinear / 12.92);
            float b = (bLinear > 0.04045) ? (float)Math.Pow((bLinear + 0.055) / (1 + 0.055), 2.2) : (float)(bLinear / 12.92);

            // converts
            return new Vector3(
                (r * 0.4124f + g * 0.3576f + b * 0.1805f),
                (r * 0.2126f + g * 0.7152f + b * 0.0722f),
                (r * 0.0193f + g * 0.1192f + b * 0.9505f)
                );
        }
        public static Vector3 XYZVectorToRGBVector(this Vector3 xyz)
        {
            var x = xyz.X;
            var y = xyz.Y;
            var z = xyz.Z;
            float[] Clinear = new float[3];
            Clinear[0] = x * 3.2410f - y * 1.5374f - z * 0.4986f; // red
            Clinear[1] = -x * 0.9692f + y * 1.8760f - z * 0.0416f; // green
            Clinear[2] = x * 0.0556f - y * 0.2040f + z * 1.0570f; // blue

            for (int i = 0; i < 3; i++)
            {
                Clinear[i] = (Clinear[i] <= 0.0031308) ? 12.92f * Clinear[i] : (float)((
                    1 + 0.055) * Math.Pow(Clinear[i], (1.0 / 2.4)) - 0.055);
            }

            return new Vector3(
                Convert.ToInt32(float.Parse(string.Format("{0:0.00}",
                    Clinear[0] * 255.0))),
                Convert.ToInt32(float.Parse(string.Format("{0:0.00}",
                    Clinear[1] * 255.0))),
                Convert.ToInt32(float.Parse(string.Format("{0:0.00}",
                    Clinear[2] * 255.0)))
                );
        }
        private static float D65X = 0.9505f;
        private static float D65Y = 1f;
        private static float D65Z = 1.089f;
        private static float Fxyz(float t)
        {
            return ((t > 0.008856) ? (float)Math.Pow(t, (1.0 / 3.0)) : (7.787f * t + 16.0f / 116.0f));
        }
        public static Vector3 XYZVectorToLABVector(this Vector3 xyz)
        {
            Vector3 lab = new Vector3();
            var x = xyz.X;
            var y = xyz.Y;
            var z = xyz.Z;
            lab.X = 116.0f * Fxyz(y / D65Y) - 16f;
            lab.Y = 500.0f * (Fxyz(x / D65X) - Fxyz(y / D65Y));
            lab.Z = 200.0f * (Fxyz(y / D65Y) - Fxyz(z / D65Z));
            return lab;
        }
        public static Vector3 LABVectorToXYZVector(this Vector3 lab)
        {
            float delta = 6.0f / 29.0f;
            var l = lab.X;
            var a = lab.Y;
            var b = lab.Z;
            float fy = (l + 16f) / 116.0f;
            float fx = fy + (a / 500.0f);
            float fz = fy - (b / 200.0f);

            return new Vector3(
                (fx > delta) ? D65X * (fx * fx * fx) : (fx - 16.0f / 116.0f) * 3 * (
                    delta * delta) * D65X,
                (fy > delta) ? D65Y * (fy * fy * fy) : (fy - 16.0f / 116.0f) * 3 * (
                    delta * delta) * D65Y,
                (fz > delta) ? D65Z * (fz * fz * fz) : (fz - 16.0f / 116.0f) * 3 * (
                    delta * delta) * D65Z
                );
        }

        public static Vector3 RGBVectorToLABVector(this Vector3 rgb)
        {
            return rgb.RGBVectorToXYZVector().XYZVectorToLABVector();
        }
        public static Vector3 LABVectorToRGBVector(this Vector3 lab)
        {
            return lab.LABVectorToXYZVector().XYZVectorToRGBVector();
        }

        internal static float A = 0.17883277f;
        internal static float B = 0.28466892f;
        internal static float C = 0.55991073f;
        internal static float HLGGap = 1f / 12f;
        internal static float HLGFunction1(float s)
        {
            return 0.5f * (float)Math.Sqrt(12f * s);
        }
        internal static float HLGFunction2(float s)
        {
            return (float)(A * Math.Log(12f * s - B)) + C;
        }

        public static bool HLGColorIsDark(this HSVColor color)
        {
            if (color.V < 65) return true;
            var s = color.S / 100;
            if (s <= HLGGap)
            {
                var targetV = HLGFunction1(s);
                return color.V / 100f < targetV;
            }
            else
            {
                var targetV = HLGFunction2(s);
                return color.V / 100f < targetV;
            }
        }

        internal static float BT709Gap = 0.018f;
        internal static float BT709Function1(float s)
        {
            return 4.5f * s;
        }
        internal static float BT709Function2(float s)
        {
            return (float)(1.099 * Math.Pow(s, 0.45) - 0.099);
        }
        public static bool BT709ColorIsDark(this HSVColor color)
        {
            if (color.V < 65) return true;
            var s = color.S / 100;
            if (s <= BT709Gap)
            {
                var targetV = BT709Function1(s);
                return color.V / 100f < targetV;
            }
            else
            {
                var targetV = BT709Function2(s);
                return color.V / 100f < targetV;
            }
        }

        internal static float sRGBGap = 0.0031308f;
        internal static float sRGBFunction1(float s)
        {
            return 12.92f * s;
        }
        internal static float sRGBFunction2(float s)
        {
            return (float)(1.055 * Math.Pow(s, 1 / 2.4) - 0.055);
        }
        public static bool sRGBColorIsDark(this HSVColor color)
        {
            if (color.V < 65) return true;
            var s = color.S / 100;
            if (s <= sRGBGap)
            {
                var targetV = sRGBFunction1(s);
                return color.V / 100f < targetV;
            }
            else
            {
                var targetV = sRGBFunction2(s);
                return color.V / 100f < targetV;
            }
        }

        public static bool RGBVectorLStarIsDark(this Vector3 rgb)
        {
            var limitedColor = rgb / 255f;
            var y = 0.2126f * ChannelToLin(limitedColor.X) + 0.7152f * ChannelToLin(limitedColor.Y) + 0.0722f * ChannelToLin(limitedColor.Z);
            var lStar = YToLStar(y);
            return lStar <= 55;
        }
        public static float ChannelToLin(float value)
        {
            if (value <= 0.04045f)
            {
                return value / 12.92f;
            }
            else
            {
                return (float)Math.Pow((value + 0.055) / 1.055, 2.4);
            }
        }
        public static float YToLStar(float y)
        {
            if (y <= (216f / 24389f))
            {       // The CIE standard states 0.008856 but 216/24389 is the intent for 0.008856451679036
                return y * (24389f / 27f);  // The CIE standard states 903.3, but 24389/27 is the intent, making 903.296296296296296
            }
            else
            {
                return (float)Math.Pow(y, (1f / 3f)) * 116f - 16f;
            }
        }
    }
}
