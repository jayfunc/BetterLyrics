using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Graphics.Canvas.Text;

namespace BetterLyrics.WinUI3.Helpers;

public static class FontHelper
{
    private static List<ExtendedFontFamily>? _fontCache;

    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    private static readonly IAppUIThreadProvider _appUIThreadProvider =
        Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    public static async Task<List<ExtendedFontFamily>> GetSystemFontFamiliesAsync()
    {
        if (_fontCache != null) return _fontCache;

        await _cacheLock.WaitAsync();
        try
        {
            if (_fontCache != null) return _fontCache;

            var (EnglishNames, LocalNames) = await GetRawDataOnUIThreadAsync();

            if (EnglishNames == null || LocalNames == null || EnglishNames.Length == 0) return [];

            if (EnglishNames.Length != LocalNames.Length)
                Debug.WriteLine("Warning: Font list lengths differ between Locales!");

            _fontCache = await Task.Run(() =>
            {
                return EnglishNames
                    .Zip(LocalNames, (en, loc) => new ExtendedFontFamily
                    {
                        FontFamily = en,
                        LocalizedFontFamily = loc
                    })
                    .OrderBy(f => f.LocalizedFontFamily)
                    .ToList();
            });

            return _fontCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static Task<(string[] EnglishNames, string[] LocalNames)> GetRawDataOnUIThreadAsync()
    {
        var tcs = new TaskCompletionSource<(string[], string[])>();

        _appUIThreadProvider.Execute(() =>
        {
            try
            {
                var enNames = CanvasTextFormat.GetSystemFontFamilies(new[] { "en-us" });

                var greedyLocales = new List<string>
                {
                    CultureInfo.CurrentUICulture.Name
                };

                if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh",
                        StringComparison.OrdinalIgnoreCase))
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