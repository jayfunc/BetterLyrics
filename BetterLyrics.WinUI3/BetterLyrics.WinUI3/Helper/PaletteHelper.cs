using ColorThiefDotNet;
using CommunityToolkit.WinUI.Helpers;
using Impressionist.Abstractions;
using Impressionist.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PaletteHelper
    {
        private static ColorThief colorThief = new();
        public static async Task<PaletteResult> OctTreeGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool? isDark = null)
        {
            var colors = await GetPixelColor(decoder);
            var palette = await PaletteGenerators.OctTreePaletteGenerator.CreatePalette(colors, count, false, isDark);
            return palette;
        }

        public static async Task<ThemeColorResult> OctTreeGetAccentColorFromByteAsync(BitmapDecoder decoder)
        {
            var colors = await GetPixelColor(decoder);
            var theme = await PaletteGenerators.OctTreePaletteGenerator.CreateThemeColor(colors, false);
            return theme;
        }

        public static async Task<ThemeColorResult> MedianCutGetAccentColorFromByteAsync(BitmapDecoder decoder)
        {
            var mainColor = await colorThief.GetColor(decoder, 10, false);
            var theme = new ThemeColorResult(new Vector3(mainColor.Color.R, mainColor.Color.G, mainColor.Color.B), mainColor.IsDark);
            return theme;
        }

        public static async Task<PaletteResult> MedianCutGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool? isDark = null)
        {
            var mainColor = await colorThief.GetColor(decoder, 10, false);
            var theme = new ThemeColorResult(new Vector3(mainColor.Color.R, mainColor.Color.G, mainColor.Color.B), mainColor.IsDark);
            var palette = await colorThief.GetPalette(decoder, 255, 10, false);
            var topColors = palette
                .Where(x => x.IsDark == (isDark ?? mainColor.IsDark))
                .OrderByDescending(x => x.Population)
                .Select(x => new Vector3(x.Color.R, x.Color.G, x.Color.B))
                .Take(count)
                .ToList();
            var paletteResult = new PaletteResult(topColors, mainColor.IsDark, theme);

            return paletteResult;
        }

        public static List<Windows.UI.Color> GenerateChartColors(Windows.UI.Color baseColor, int count)
        {
            List<Windows.UI.Color> results = [];

            var baseHsl = baseColor.ToHsl();
            double baseHue = baseHsl.H;
            double baseSaturation = baseHsl.S;
            double baseBrightness = baseHsl.L;

            double step = 360.0 / count;

            for (int i = 0; i < count; i++)
            {
                double newHue = (baseHue + (step * i)) % 360;

                Windows.UI.Color newColor = CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(newHue, baseSaturation, baseBrightness);
                results.Add(newColor);
            }

            return results;
        }

        private static async Task<Dictionary<Vector3, int>> GetPixelColor(BitmapDecoder bitmapDecoder)
        {
            var pixelDataProvider = await bitmapDecoder.GetPixelDataAsync();
            var pixels = pixelDataProvider.DetachPixelData();
            var count = bitmapDecoder.PixelWidth * bitmapDecoder.PixelHeight;
            var vector = new Dictionary<Vector3, int>();
            for (int i = 0; i < count; i += 10)
            {
                var offset = i * 4;
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];
                if (a == 0) continue;
                var color = new Vector3(r, g, b);
                if (vector.ContainsKey(color))
                {
                    vector[color]++;
                }
                else
                {
                    vector[color] = 1;
                }
            }
            return vector;
        }
    }
}
