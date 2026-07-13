using BetterLyrics.Core.Enums;
using Impressionist.Abstractions;
using Impressionist.Implementations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace BetterLyrics.Core.Helpers
{
    public static class PaletteHelper
    {
        public static Task<PaletteResult> GetAccentColorsAsync(byte[]? data, int count, PaletteGeneratorType generatorType, bool? isDark)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => OctTreeGetAccentColorsFromByteAsync(data, count, isDark),
                PaletteGeneratorType.KMeans => KMeansGetAccentColorsFromByteAsync(data, count, isDark),
                PaletteGeneratorType.Auto => AutoGetAccentColorsFromByteAsync(data, count, isDark),
                _ => AutoGetAccentColorsFromByteAsync(data, count, isDark),
            };
        }

        public static async Task<PaletteResult> OctTreeGetAccentColorsFromByteAsync(byte[]? data, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(data);
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

        public static async Task<PaletteResult> KMeansGetAccentColorsFromByteAsync(byte[]? data, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(data);
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

        public static async Task<PaletteResult> AutoGetAccentColorsFromByteAsync(byte[]? data, int count, bool? isDark)
        {
            var colors = await GetPixelColorAsync(data);
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

        private static async Task<Dictionary<Vector3, int>> GetPixelColorAsync(byte[]? data)
        {
            var colorCounts = new Dictionary<Vector3, int>();

            if (data == null || data.Length == 0)
            {
                return colorCounts;
            }

            return await Task.Run(() =>
            {
                using (Image<Rgba32> image = Image.Load<Rgba32>(data))
                {
                    int width = image.Width;
                    int height = image.Height;

                    // 每隔 10 个像素采样一次
                    int sampleStep = 10;

                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < height; y++)
                        {
                            var rowSpan = accessor.GetRowSpan(y);

                            for (int x = 0; x < width; x += sampleStep)
                            {
                                Rgba32 pixel = rowSpan[x];

                                if (pixel.A == 0) continue;

                                var color = new Vector3(pixel.R, pixel.G, pixel.B);

                                if (colorCounts.TryGetValue(color, out int count))
                                {
                                    colorCounts[color] = count + 1;
                                }
                                else
                                {
                                    colorCounts[color] = 1;
                                }
                            }
                        }
                    });
                }

                return colorCounts;
            });
        }
    }
}
