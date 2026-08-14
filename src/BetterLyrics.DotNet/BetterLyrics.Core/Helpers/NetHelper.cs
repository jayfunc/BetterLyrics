namespace BetterLyrics.Core.Helpers;

public class NetHelper
{
    public static async Task<bool> CheckConnectivityAsync(string url)
    {
        try
        {
            var httpClientFactory = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IHttpClientFactory>();
            using var client = httpClientFactory != null ? httpClientFactory.CreateClient() : new HttpClient();
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