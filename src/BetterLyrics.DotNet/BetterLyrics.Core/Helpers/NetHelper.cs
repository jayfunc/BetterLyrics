namespace BetterLyrics.Core.Helpers;

public class NetHelper
{
    public static async Task<bool> CheckConnectivityAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
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