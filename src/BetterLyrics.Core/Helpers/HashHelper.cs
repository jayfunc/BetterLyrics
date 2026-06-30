using System.IO.Hashing;
using System.Text;

namespace BetterLyrics.Core.Helpers
{
    public static class HashHelper
    {
        public static int GetSafeHash(string str, int min)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            byte[] hash = XxHash32.Hash(bytes);

            uint rawValue = BitConverter.ToUInt32(hash, 0);

            int minValue = min;
            int maxValue = int.MaxValue;

            uint range = (uint)maxValue - (uint)minValue;

            int finalId = (int)((rawValue % range) + minValue);

            return finalId;
        }
    }
}
