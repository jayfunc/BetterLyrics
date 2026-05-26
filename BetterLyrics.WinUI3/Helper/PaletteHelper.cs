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
        public static async Task<PaletteResult> OctTreeGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            if (isDark != null)
            {
                colors = colors
                    .Where(x => x.Key.PaletteRGBVectorLStarIsDark() == isDark)
                    .ToDictionary(x => x.Key, x => x.Value);
                if (colors.Count == 0)
                {
                    colors.Add(isDark.Value ? Vector3.Zero : new Vector3(255, 255, 255), 1);
                }
            }
            var palette = await OctTreePaletteGenerator.CreatePalette(colors, count);
            return palette;
        }

        public static async Task<PaletteResult> KMeansGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            if (isDark != null)
            {
                colors = colors
                    .Where(x => x.Key.PaletteRGBVectorLStarIsDark() == isDark)
                    .ToDictionary(x => x.Key, x => x.Value);
                if (colors.Count == 0)
                {
                    colors.Add(isDark.Value ? Vector3.Zero : new Vector3(255, 255, 255), 1);
                }
            }
            var palette = await KMeansPaletteGenerator.CreatePalette(colors, count);
            return palette;
        }

        public static async Task<PaletteResult> AutoGetAccentColorsFromByteAsync(BitmapDecoder decoder, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(decoder);
            if (isDark != null)
            {
                colors = colors
                    .Where(x => x.Key.PaletteRGBVectorLStarIsDark() == isDark)
                    .ToDictionary(x => x.Key, x => x.Value);
                if (colors.Count == 0)
                {
                    colors.Add(isDark.Value ? Vector3.Zero : new Vector3(255, 255, 255), 1);
                }
            }
            var palette = await AutoPaletteGenerator.CreatePalette(colors, count);
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
