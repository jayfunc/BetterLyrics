using BetterLyrics.Core.Enums;
using Impressionist;
using Impressionist.Helpers;
using Impressionist.Helpers.Hct;
using Impressionist.Quantizers;
using Impressionist.Selectors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace BetterLyrics.Core.Helpers
{
    public static class PaletteHelper
    {
        private static async Task<List<ArgbColor>> GetPixelsAsync(byte[]? data)
        {
            var pixels = new List<ArgbColor>();
            if (data == null || data.Length == 0) return pixels;
            return await Task.Run(() =>
            {
                using (Image<Rgba32> image = Image.Load<Rgba32>(data))
                {
                    int width = image.Width;
                    int height = image.Height;
                    int sampleStep = Math.Max(1, Math.Max(width, height) / 64); // 10
                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < height; y += sampleStep)
                        {
                            var rowSpan = accessor.GetRowSpan(y);
                            for (int x = 0; x < width; x += sampleStep)
                            {
                                Rgba32 pixel = rowSpan[x];
                                if (pixel.A == 0) continue;

                                pixels.Add(new ArgbColor(255, pixel.R, pixel.G, pixel.B));
                            }
                        }
                    });
                }
                return pixels;
            });
        }

        public static async Task<(List<Vector3> Accent, List<Vector3> Light, List<Vector3> Dark)> GetThemeColorsBatchAsync(
            byte[]? data, int count, PaletteGeneratorType generatorType,
            double chromaWeight = 1.0, double toneWeight = -0.75, double populationWeight = 3.0,
            double darkToneThreshold = 75.0, double lightToneThreshold = 25.0)
        {
            var pixels = await GetPixelsAsync(data);
            if (pixels.Count == 0)
            {
                return ([], [], []);
            }
            IQuantizer quantizer = generatorType switch
            {
                PaletteGeneratorType.WuQuantizer => new WuQuantizer(),
                PaletteGeneratorType.WsMeansQuantizer => new WsMeansQuantizer(),
                PaletteGeneratorType.CelebiQuantizer => new CelebiQuantizer(),
                _ => new CelebiQuantizer(),
            };

            var quantizerResult = quantizer.Quantize(pixels, count * 3);
            var selector = new HctColorSelector
            {
                ChromaWeight = chromaWeight,
                ToneWeight = toneWeight,
                PopulationWeight = populationWeight
            };

            var finalAccentColors = selector.SelectColors(quantizerResult.Colors, count)
                .Select(c => new Vector3(c.Red, c.Green, c.Blue)).ToList();

            var lightDict = quantizerResult.Colors
                .Where(x => Hct.From(x.Key).Tone >= lightToneThreshold)
                .ToDictionary(x => x.Key, x => x.Value);
            if (lightDict.Count == 0) lightDict.Add(new ArgbColor(255, 255, 255, 255), 1);
            var finalLightColors = selector.SelectColors(lightDict, count)
                .Select(c => new Vector3(c.Red, c.Green, c.Blue)).ToList();

            var darkDict = quantizerResult.Colors
                .Where(x => Hct.From(x.Key).Tone < darkToneThreshold)
                .ToDictionary(x => x.Key, x => x.Value);
            if (darkDict.Count == 0) darkDict.Add(new ArgbColor(255, 0, 0, 0), 1);
            var finalDarkColors = selector.SelectColors(darkDict, count)
                .Select(c => new Vector3(c.Red, c.Green, c.Blue)).ToList();

            return (finalAccentColors, finalLightColors, finalDarkColors);
        }

        public static async Task<List<Vector3>> GetAccentColorsAsync(
            byte[]? data, int count, PaletteGeneratorType generatorType, bool? isDark, 
            double chromaWeight = 1.0, double toneWeight = -0.75, double populationWeight = 3.0, 
            double darkToneThreshold = 75.0, double lightToneThreshold = 25.0)
        {
            var pixels = await GetPixelsAsync(data);
            if (pixels.Count == 0) return new List<Vector3>();

            IQuantizer quantizer = generatorType switch
            {
                PaletteGeneratorType.WuQuantizer => new WuQuantizer(),
                PaletteGeneratorType.WsMeansQuantizer => new WsMeansQuantizer(),
                PaletteGeneratorType.CelebiQuantizer => new CelebiQuantizer(),
                _ => new CelebiQuantizer(),
            };

            var quantizerResult = quantizer.Quantize(pixels, count * 2);

            var filteredDict = quantizerResult.Colors;
            if (isDark != null)
            {
                var threshold = isDark.Value ? darkToneThreshold : lightToneThreshold;
                filteredDict = quantizerResult.Colors
                    .Where(x => (Hct.From(x.Key).Tone < threshold) == isDark)
                    .ToDictionary(x => x.Key, x => x.Value);

                // fallback
                if (filteredDict.Count == 0)
                {
                    filteredDict.Add(isDark.Value ? new ArgbColor(255, 0, 0, 0) : new ArgbColor(255, 255, 255, 255), 1);
                }
            }

            var selector = new HctColorSelector
            {
                ChromaWeight = chromaWeight,
                ToneWeight = toneWeight,
                PopulationWeight = populationWeight
            };
            var finalArgbColors = selector.SelectColors(filteredDict, count);

            return finalArgbColors.Select(c => new Vector3(c.Red, c.Green, c.Blue)).ToList();
        }
    }
}
