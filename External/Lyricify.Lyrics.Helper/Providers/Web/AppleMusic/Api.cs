using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Providers.Web.AppleMusic
{
    /// <summary>
    /// Apple Music API (amp-api.music.apple.com)
    /// - AccessToken 通过抓取 https://music.apple.com/us/browse 的 index*.js 得到
    /// - Media User Token 外界通过 SetMediaUserToken 注入，用于 /v1/me/storefront 获取 storefront/language
    /// - 如果没有 Media User Token，则 fallback 到 storefront=us, language=en-US
    /// </summary>
    public class Api : BaseApi
    {
        protected override string? HttpRefer => "https://music.apple.com/";

        // BaseApi 每次请求都会 Clear headers，所以我们把“动态 header”都通过 AdditionalHeaders 注入
        protected override Dictionary<string, string>? AdditionalHeaders
        {
            get
            {
                EnsureInitSync(); // 只做轻量检查；真正网络初始化在 EnsureInitAsync
                var dict = new Dictionary<string, string>
                {
                    { "Origin", "https://music.apple.com" },
                    { "Accept", "application/json" },
                };

                if (!string.IsNullOrWhiteSpace(_accessToken))
                    dict["Authorization"] = $"Bearer {_accessToken}";

                if (!string.IsNullOrWhiteSpace(_acceptLanguage))
                    dict["Accept-Language"] = _acceptLanguage!;

                if (!string.IsNullOrWhiteSpace(_mediaUserToken))
                    dict["media-user-token"] = _mediaUserToken!;

                return dict;
            }
        }

        // ====== 缓存状态（做成静态：全局复用 token/storefront/language）======
        private static readonly object _lock = new();

        private static bool _inited = false;
        private static string _accessToken = string.Empty;

        private static string _storefront = "us";
        private static string _language = "en-US";
        private static string? _acceptLanguage = "en-US,en;q=0.9";

        // MediaUserToken 允许外界注入；变化时会触发重新 init storefront/language
        private static string? _mediaUserToken;
        private static string? _cachedMut;

        /// <summary>
        /// 对外提供：导入 Media User Token
        /// </summary>
        public void SetMediaUserToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            lock (_lock)
            {
                if (token.Trim() == _mediaUserToken)
                {
                    // Token 未变化，无需重置 _inited
                    return;
                }

                _mediaUserToken = token.Trim();
                _inited = false; // token 变化，下一次请求会刷新 storefront/language
            }
        }

        private static void EnsureInitSync()
        {
            // 这里不做网络请求，只保证字段存在（避免 AdditionalHeaders 里引用空）
            if (string.IsNullOrWhiteSpace(_storefront)) _storefront = "us";
            if (string.IsNullOrWhiteSpace(_language)) _language = "en-US";
            if (string.IsNullOrWhiteSpace(_acceptLanguage)) _acceptLanguage = $"{_language},en;q=0.9";
        }

        private async Task EnsureInitAsync()
        {
            string? mut;
            bool needInit;
            lock (_lock)
            {
                mut = _mediaUserToken;
                needInit = !_inited || string.IsNullOrEmpty(_accessToken) || !string.Equals(_cachedMut, mut, StringComparison.Ordinal);
            }

            if (!needInit) return;

            // 1) 获取 access token
            var accessToken = await GetAccessTokenAsync().ConfigureAwait(false);
            lock (_lock)
            {
                _accessToken = accessToken;
            }

            // 2) 若存在 media-user-token 则获取 storefront/language，否则 fallback
            if (!string.IsNullOrWhiteSpace(mut))
            {
                try
                {
                    await FetchStorefrontAsync(mut!).ConfigureAwait(false);
                }
                catch
                {
                    lock (_lock)
                    {
                        _storefront = "us";
                        _language = "en-US";
                        _acceptLanguage = $"{_language},en;q=0.9";
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    _storefront = "us";
                    _language = "en-US";
                    _acceptLanguage = $"{_language},en;q=0.9";
                }
            }

            lock (_lock)
            {
                _cachedMut = mut;
                _inited = true;
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // 注意：BaseApi.GetAsync 会清 header，所以这里用同一个 Api 调即可
            // 先抓 browse HTML
            var html = await GetAsync("https://music.apple.com/us/browse").ConfigureAwait(false);

            // 找 index*.js（更鲁棒）
            var jsMatch = Regex.Match(html, "assets/index(?<hash>[^\"']+)\\.js", RegexOptions.IgnoreCase);
            string jsUrl;

            if (jsMatch.Success)
                jsUrl = $"https://music.apple.com/assets/index{jsMatch.Groups["hash"].Value}.js";
            else
            {
                // 兜底
                var m2 = Regex.Match(html, "(?<=index)(.*?)(?=\\.js\\\")");
                if (!m2.Success) throw new Exception("AppleMusic: Failed to find index*.js");
                jsUrl = $"https://music.apple.com/assets/index{m2.Value}.js";
            }

            // 抓 js
            var js = await GetAsync(jsUrl).ConfigureAwait(false);

            // 找 JWT（通常以 eyJh 开头）
            var tokenMatch = Regex.Match(js, "(?=eyJh)(.*?)(?=\")");
            if (!tokenMatch.Success) throw new Exception("AppleMusic: Failed to find access token");

            return tokenMatch.Value;
        }

        private async Task FetchStorefrontAsync(string mediaUserToken)
        {
            // 这里必须带 Authorization + media-user-token
            // 因为 BaseApi 每次 Clear header，所以本方法开头先把 _mediaUserToken 写入静态字段（让 AdditionalHeaders 注入）
            lock (_lock) _mediaUserToken = mediaUserToken;

            var json = await GetAsync("https://amp-api.music.apple.com/v1/me/storefront").ConfigureAwait(false);
            var resp = JsonConvert.DeserializeObject<StorefrontResponse>(json);

            var data = resp?.Data;
            if (data == null || data.Length == 0) throw new Exception("AppleMusic: storefront data empty");

            var storefront = data[0].Id;
            var language = data[0].Attributes?.DefaultLanguageTag;

            if (string.IsNullOrWhiteSpace(storefront)) throw new Exception("AppleMusic: invalid storefront");
            if (string.IsNullOrWhiteSpace(language)) language = "en-US";

            lock (_lock)
            {
                _storefront = storefront!;
                _language = language!;
                _acceptLanguage = $"{_language},en;q=0.9";
            }
        }

        public async Task<SearchResponse?> Search(string keyword, int limit = 20)
        {
            await EnsureInitAsync().ConfigureAwait(false);

            string storefront, language;
            lock (_lock) { storefront = _storefront; language = _language; }

            var url =
                $"https://amp-api.music.apple.com/v1/catalog/{storefront}/search" +
                $"?term={WebUtility.UrlEncode(keyword)}&types=songs&limit={limit}&l={WebUtility.UrlEncode(language)}";

            var json = await GetAsync(url).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SearchResponse>(json);
        }

        /// <summary>
        /// 获取逐字歌词（优先 syllable-lyrics.ttml），返回的 LyricResponse 会把 Ttml 归一化到 resp.Ttml
        /// </summary>
        public async Task<LyricResponse?> GetLyrics(string songId)
        {
            await EnsureInitAsync().ConfigureAwait(false);

            string storefront, language;
            lock (_lock) { storefront = _storefront; language = _language; }

            //var url =
            //    $"https://amp-api.music.apple.com/v1/catalog/{storefront}/songs/{songId}" +
            //    $"?include[songs]=syllable-lyrics&l={WebUtility.UrlEncode(language)}&extend=ttmlLocalizations";

            var url =
                $"https://amp-api.music.apple.com/v1/catalog/{storefront}/songs/{songId}" +
                $"?include[songs]=syllable-lyrics&l={WebUtility.UrlEncode("zh-hans-cn")}&extend=ttmlLocalizations";

            var json = await GetAsync(url).ConfigureAwait(false);
            var resp = JsonConvert.DeserializeObject<LyricResponse>(json);

            resp?.NormalizeTtml();
            return resp;
        }
    }
}
