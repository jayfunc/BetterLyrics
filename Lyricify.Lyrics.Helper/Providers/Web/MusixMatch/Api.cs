using Newtonsoft.Json;

namespace Lyricify.Lyrics.Providers.Web.Musixmatch
{
    public class Api : BaseApi
    {
        protected override string? HttpRefer => null;

        protected override Dictionary<string, string>? AdditionalHeaders =>
            new()
            {
                { "authority", "apic-desktop.musixmatch.com" }
            };

        private string? _userToken { get; set; } = null;

        /// <summary>
        /// 设置 UserToken (例如之前缓存的 Token)
        /// </summary>
        public void SetUserToken(string token)
        {
            _userToken = token;
        }

        /// <summary>
        /// 获取当前的 UserToken (例如用于缓存)
        /// </summary>
        public string? GetUserToken()
        {
            return _userToken;
        }

        public async Task<GetTokenResponse?> GetToken()
        {
            var response = await GetAsync("https://apic-desktop.musixmatch.com/ws/1.1/token.get?app_id=web-desktop-app-v1.0&t=" + RandomId());
            var resp = JsonConvert.DeserializeObject<GetTokenResponse>(response);
            return resp;
        }

        /// <summary>
        /// 获取曲目
        /// </summary>
        /// <param name="track">曲目</param>
        /// <param name="artist">艺人</param>
        /// <param name="duration">曲目市场 (秒)</param>
        public async Task<GetTrackResponse?> GetTrack(string track, string artist, int? duration = null)
        {
            await EnsureUserToken();

            var response = await MusixmatchGetAsync("matcher.track.get" +
                $"?q_track={track}" +
                $"&q_artist={artist}" +
                (duration.HasValue ? $"&q_duration={duration}" : string.Empty));
            if (response == null) return null;
            return JsonConvert.DeserializeObject<GetTrackResponse>(response);
        }

        /// <summary>
        /// 获取完整歌词
        /// </summary>
        /// <param name="track">曲目</param>
        /// <param name="artist">艺人</param>
        /// <param name="duration">曲目市场 (秒)</param>
        public async Task<GetTrackResponse?> GetFullLyrics(string track, string artist, int? duration = null)
        {
            await EnsureUserToken();

            var response = await GetFullLyricsRaw(track, artist, duration);
            if (response == null) return null;
            return JsonConvert.DeserializeObject<GetTrackResponse>(response);
        }

        /// <summary>
        /// 获取完整歌词 (返回的原始字符串)
        /// </summary>
        /// <param name="trackId">曲目 ID</param>
        /// <returns>接口返回的原始字符串，可直接用 MusixmatchParser 解析</returns>
        public async Task<string?> GetFullLyricsRaw(string trackId)
        {
            await EnsureUserToken();

            var response = await MusixmatchGetAsync("macro.subtitles.get?namespace=lyrics_richsynched&optional_calls=track.richsync&subtitle_format=lrc" +
                $"&track_id={trackId}" +
                "&f_subtitle_length_max_deviation=40");
            return response;
        }

        /// <summary>
        /// 获取完整歌词 (返回的原始字符串)
        /// </summary>
        /// <param name="track">曲目</param>
        /// <param name="artist">艺人</param>
        /// <param name="duration">曲目市场 (秒)</param>
        /// <returns>接口返回的原始字符串，可直接用 MusixmatchParser 解析</returns>
        public async Task<string?> GetFullLyricsRaw(string track, string artist, int? duration = null)
        {
            await EnsureUserToken();

            var response = await MusixmatchGetAsync("macro.subtitles.get?namespace=lyrics_richsynched&optional_calls=track.richsync&subtitle_format=lrc" +
                $"&q_track={track}" +
                $"&q_artist={artist}" +
                (duration.HasValue ? $"&f_subtitle_length={duration}&q_duration={duration}" : string.Empty) +
                "&f_subtitle_length_max_deviation=40");
            return response;
        }

        /// <summary>
        /// 获取翻译
        /// </summary>
        /// <param name="trackId">曲目 ID</param>
        /// <param name="language">翻译的语言</param>
        public async Task<GetTranslationsResponse?> GetTranslations(string trackId, string language = "zh")
        {
            var response = await MusixmatchGetAsync("crowd.track.translations.get?translation_fields_set=minimal" +
                $"&selected_language={language}" +
                $"&track_id={trackId}" +
                $"&comment_format=text&part=user");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<GetTranslationsResponse>(response);
        }

        /// <summary>
        /// Musixmatch GET 请求
        /// </summary>
        /// <param name="req">URL 部分</param>
        /// <returns></returns>
        private async Task<string?> MusixmatchGetAsync(string req, int maxTrial = 8)
        {
            if (--maxTrial < 0) return null;

            await EnsureUserToken();

            var url = "https://apic-desktop.musixmatch.com/ws/1.1/" +
                req +
                $"&usertoken={_userToken}" +
                "&format=json" +
                "&app_id=web-desktop-app-v1.0" +
                "&t=" + RandomId();

            var response = await GetAsync(url);

            if (response?.Contains("\"status_code\":401") == true)
            {
                if (response.Contains("\"hint\":\"renew\"") == true)
                {
                    _userToken = null;
                    response = await MusixmatchGetAsync(req, maxTrial) ?? response;
                }
                else if (response.Contains("\"hint\":\"captcha\"") == true)
                {
                    await Task.Delay(1000);
                    response = await MusixmatchGetAsync(req, maxTrial) ?? response;
                }
            }

            if (response?.Contains("\"status_code\":401") == true
                && response.Contains("\"hint\":\"captcha\"") == true)
            {
                throw new RequestCaptchaException(url, response);
            }

            return response;
        }

        /// <summary>
        /// 生成请求随机参数
        /// </summary>
        private static string RandomId()
        {
            var code = ConvertToBase36((long)(new Random().NextDouble() * long.MaxValue));
            code = new string(code.Where(char.IsLetter).ToArray());
            return code.Substring(2, Math.Min(8, code.Length - 2));

            static string ConvertToBase36(long value)
            {
                const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
                int radix = chars.Length;
                char[] result = new char[13];
                int index = 12;

                do
                {
                    result[index--] = chars[(int)(value % radix)];
                    value /= radix;
                } while (value > 0 && index >= 0);

                return new string(result, index + 1, 12 - index);
            }
        }

        /// <summary>
        /// 确保有 UserToken
        /// </summary>
        private async Task EnsureUserToken()
        {
            if (_userToken != null) return;
            await RefreshUserToken(true);
        }

        /// <summary>
        /// 刷新 UserToken，可用于失效时的刷新
        /// </summary>
        /// <returns></returns>
        private async Task RefreshUserToken(bool isEnsure = false)
        {
            var response = await GetToken();
            int maxTry = 10;
            while (response?.Message.Header.StatusCode == 401 && response?.Message.Header.Hint == "captcha" && maxTry-- > 0)
            {
                await Task.Delay(1000);
                if (isEnsure && !string.IsNullOrEmpty(_userToken)) return;
                response = await GetToken();
            }
            if (string.IsNullOrEmpty(response?.Message?.Body?.UserToken))
                throw new Exception("User Token failed to refresh");

            _userToken = response?.Message?.Body?.UserToken;
        }
    }

    public class RequestCaptchaException : Exception
    {
        public const string DefaultMessage = "Hit 401 error with Captcha hint.";

        public RequestCaptchaException() : base(DefaultMessage) { }

        public RequestCaptchaException(string requestUrl, string response) : base(DefaultMessage)
        {
            RequestUrl = requestUrl;
            Response = response;
        }

        public RequestCaptchaException(Exception innerException) : base(DefaultMessage, innerException) { }

        public string? RequestUrl { get; private set; }

        public string? Response { get; private set; }
    }
}
