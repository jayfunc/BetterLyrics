namespace BetterLyrics.Core.Models.Domain;

public struct AppColor : IEquatable<AppColor>
{
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public AppColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public override bool Equals(object? obj)
    {
        return obj is AppColor other && Equals(other);
    }

    public bool Equals(AppColor appColor)
    {
        return this == appColor;
    }

    public static bool operator ==(AppColor appColor1, AppColor appColor2)
    {
        if (appColor1.R == appColor2.R && appColor1.G == appColor2.G && appColor1.B == appColor2.B)
            return appColor1.A == appColor2.A;

        return false;
    }

    public static bool operator !=(AppColor appColor1, AppColor appColor2)
    {
        return !(appColor1 == appColor2);
    }

    public override int GetHashCode()
    {
        return A.GetHashCode() ^ R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
    }
}