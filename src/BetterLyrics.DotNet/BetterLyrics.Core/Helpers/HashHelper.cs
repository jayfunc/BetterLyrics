using System.IO.Hashing;
using System.Text;

namespace BetterLyrics.Core.Helpers;

public static class HashHelper
{
    public static int GetSafeHash(string str, int min)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        var hash = XxHash32.Hash(bytes);

        var rawValue = BitConverter.ToUInt32(hash, 0);

        var minValue = min;
        var maxValue = int.MaxValue;

        var range = (uint)maxValue - (uint)minValue;

        var finalId = (int)(rawValue % range + minValue);

        return finalId;
    }
}