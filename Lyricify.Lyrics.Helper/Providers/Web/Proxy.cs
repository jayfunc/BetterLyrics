using System.Net;

namespace Lyricify.Lyrics.Providers.Web
{
    public static class Proxy
    {
        /// <summary>
        /// 设置代理
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void SetProxy(string host, int port, string? username, string? password)
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(host, port)
            };
            if (!string.IsNullOrEmpty(username))
                handler.Proxy.Credentials = new NetworkCredential(username, password);

            BaseApi.HttpClient = new(handler);
        }

        /// <summary>
        /// 禁用代理，不使用系统代理
        /// </summary>
        public static void DisableProxy()
        {
            var handler = new HttpClientHandler
            {
                Proxy = null,
                UseProxy = false
            };
            BaseApi.HttpClient = new(handler);
        }

        /// <summary>
        /// 清除代理设定
        /// </summary>
        public static void ClearProxy()
        {
            BaseApi.HttpClient = new();
        }
    }
}
