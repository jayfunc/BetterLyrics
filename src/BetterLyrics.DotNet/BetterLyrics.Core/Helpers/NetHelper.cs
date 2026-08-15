namespace BetterLyrics.Core.Helpers;

public class NetHelper
{
    private static readonly Lazy<HttpClient> _client = new(() =>
    {
        var httpClientFactory = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IHttpClientFactory>();
        return httpClientFactory != null ? httpClientFactory.CreateClient() : new HttpClient();
    });

    public static async Task<bool> CheckConnectivityAsync(string url)
    {
        try
        {
            // Try to reach a reliable endpoint
            var res = await _client.Value.GetAsync(url);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false; // If any exception occurs, assume no connectivity
        }
    }
}