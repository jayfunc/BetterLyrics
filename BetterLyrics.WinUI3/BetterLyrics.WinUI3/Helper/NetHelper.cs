using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class NetHelper
    {
        public static async Task<bool> CheckConnectivity(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                // Try to reach a reliable endpoint
                var res = await client.GetAsync(url);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false; // If any exception occurs, assume no connectivity
            }
        }
    }
}
