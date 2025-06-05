using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class MathHelper
    {
        public static List<int> GetAllFactors(int n)
        {
            var result = new SortedSet<int>();

            for (int i = 1; i <= Math.Sqrt(n); i++)
            {
                if (n % i == 0)
                {
                    result.Add(i);
                    result.Add(n / i);
                }
            }

            return [.. result];
        }
    }
}
