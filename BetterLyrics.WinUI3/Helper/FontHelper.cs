using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class FontHelper
    {
        private static List<ExtendedFontFamily>? _fontCache;

        public static async Task<List<ExtendedFontFamily>> GetSystemFontFamiliesAsync()
        {
            if (_fontCache != null) return _fontCache;

            var rawData = await GetRawDataOnUIThreadAsync();

            if (rawData.EnglishNames == null || rawData.LocalNames == null)
            {
                return new List<ExtendedFontFamily>();
            }

            _fontCache = await Task.Run(() =>
            {
                var list = rawData.EnglishNames
                    .Zip(rawData.LocalNames, (en, loc) => new ExtendedFontFamily
                    {
                        FontFamily = en,
                        LocalizedFontFamily = loc,
                    })
                    .OrderBy(f => f.LocalizedFontFamily)
                    .ToList();

                return list;
            });

            return _fontCache;
        }

        private static Task<(string[] EnglishNames, string[] LocalNames)> GetRawDataOnUIThreadAsync()
        {
            var tcs = new TaskCompletionSource<(string[], string[])>();
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            if (dispatcher == null)
            {
                try
                {
                    dispatcher = Microsoft.UI.Xaml.Window.Current?.DispatcherQueue;
                }
                catch { }
            }

            if (dispatcher == null)
            {
                tcs.SetException(new InvalidOperationException("无法获取 UI Dispatcher，请确保在 UI 线程调用或传入 Dispatcher"));
                return tcs.Task;
            }

            dispatcher.TryEnqueue(() =>
            {
                try
                {
                    var enNames = CanvasTextFormat.GetSystemFontFamilies(new[] { "en-us" });

                    var greedyLocales = new List<string>();

                    greedyLocales.Add(CultureInfo.CurrentUICulture.Name);

                    if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        greedyLocales.Add("zh-CN");
                        greedyLocales.Add("zh-Hans"); // 简体
                        greedyLocales.Add("zh-TW");
                        greedyLocales.Add("zh-Hant"); // 繁体
                        greedyLocales.Add("zh-HK");
                        greedyLocales.Add("zh-SG");
                    }

                    greedyLocales.Add("en-us");

                    var locNames = CanvasTextFormat.GetSystemFontFamilies(greedyLocales);

                    tcs.SetResult((enNames, locNames));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}