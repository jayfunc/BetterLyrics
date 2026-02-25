using Newtonsoft.Json;
using System.Text;

namespace Lyricify.Lyrics.Providers.Web
{
    public abstract class BaseApi
    {
        public static HttpClient HttpClient = new();

        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";

        public const string Cookie = "os=pc;osver=Microsoft-Windows-10-Professional-build-16299.125-64bit;appver=2.0.3.131777;channel=netease;__remember_me=true";

        protected abstract string? HttpRefer { get; }

        protected abstract Dictionary<string, string>? AdditionalHeaders { get; }

        protected async Task<string> GetAsync(string url)
        {
            SetRequestHeaders();

            var response = await HttpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        protected async Task<string> PostAsync(string url, Dictionary<string, string> paramDict)
        {
            SetRequestHeaders();

            var content = new FormUrlEncodedContent(paramDict);
            var response = await HttpClient.PostAsync(url, content);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        protected async Task<string> PostJsonAsync(string url, object param)
        {
            SetRequestHeaders();

            var content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(url, content);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        protected async Task<string> PostAsync(string url, Dictionary<string, object> paramDict)
        {
            SetRequestHeaders();

            var jsonContent = new StringContent(paramDict.ToJson(), Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, jsonContent);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        protected async Task<string> PostAsync(string url, string param)
        {
            SetRequestHeaders();

            var jsonContent = new StringContent(param, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, jsonContent);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

        private void SetRequestHeaders()
        {
            HttpClient.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrEmpty(UserAgent))
                HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(HttpRefer))
                HttpClient.DefaultRequestHeaders.Add("Referer", HttpRefer);
            if (!string.IsNullOrEmpty(Cookie))
                HttpClient.DefaultRequestHeaders.Add("Cookie", Cookie);

            if (AdditionalHeaders is not null)
            {
                foreach (var pair in AdditionalHeaders)
                {
                    HttpClient.DefaultRequestHeaders.Add(pair.Key, pair.Value);
                }
            }
        }
    }

    public static class JsonUtils
    {
        public static T? ToEntity<T>(this string val) => JsonConvert.DeserializeObject<T>(val);

        public static List<T>? ToEntityList<T>(this string val) => JsonConvert.DeserializeObject<List<T>>(val);

        public static string? ToJson<T>(this T entity, Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(entity, formatting);
    }
}
