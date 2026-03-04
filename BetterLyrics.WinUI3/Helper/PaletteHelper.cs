using ColorThiefDotNet;
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
        private readonly static ColorThief _colorThief = new();

        public static async Task<PaletteResult> OctTreeGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            var palette = await OctTreePaletteGenerator.CreatePaletteAsync(colors, count, isDark);
            return palette;
        }

        public static async Task<PaletteResult> KMeansGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            var palette = await KMeansPaletteGenerator.CreatePaletteAsync(colors, count, isDark);
            return palette;
        }

        public static async Task<PaletteResult> MedianCutGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool isDark)
        {
            var mainColor = await _colorThief.GetColor(decoder, 10, false);
            var theme = new ThemeColorResult(new Vector3(mainColor.Color.R, mainColor.Color.G, mainColor.Color.B), mainColor.IsDark);
            var palette = await _colorThief.GetPalette(decoder, 255, 10, false);
            var topColors = palette
                .Where(x => x.IsDark == isDark)
                .OrderByDescending(x => x.Population)
                .Select(x => new Vector3(x.Color.R, x.Color.G, x.Color.B))
                .Take(count)
                .ToList();
            var paletteResult = new PaletteResult(topColors, mainColor.IsDark, theme);

            return paletteResult;
        }

        public static async Task<PaletteResult> AutoGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            var palette = await AutoPaletteGenerator.CreatePalette(colors, count, isDark);
            return palette;
        }

        private static async Task<Dictionary<Vector3, int>> GetPixelColorAsync(BitmapDecoder bitmapDecoder)
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
