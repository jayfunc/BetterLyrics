using Impressionist.Abstractions;
using Impressionist.Implementations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PaletteHelper
    {
        public static async Task<PaletteResult> OctTreeGetAccentColorsFromByteAsync(byte[] bytes, int count, bool? isDark = null)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var colors = await GetPixelColor(decoder);
            var palette = await PaletteGenerators.OctTreePaletteGenerator.CreatePalette(colors, count, false, isDark);
            return palette;
        }

        public static async Task<ThemeColorResult> OctTreeGetAccentColorFromByteAsync(byte[] bytes)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var colors = await GetPixelColor(decoder);
            var theme = await PaletteGenerators.OctTreePaletteGenerator.CreateThemeColor(colors, false);
            return theme;
        }

        public static Task<ThemeColorResult> MedianCutGetAccentColorFromByteAsync(byte[] bytes)
        {
            using var image = Image.Load<Rgba32>(bytes);
            var colorThief = new ColorThief.ImageSharp.ColorThief();
            var mainColor = colorThief.GetColor(image, 10, false);
            var theme = new ThemeColorResult(new Vector3(mainColor.Color.R, mainColor.Color.G, mainColor.Color.B), mainColor.IsDark);
            return Task.FromResult(theme);
        }

        public static Task<PaletteResult> MedianCutGetAccentColorsFromByteAsync(byte[] bytes, int count, bool? isDark = null)
        {
            using var image = Image.Load<Rgba32>(bytes);
            var colorThief = new ColorThief.ImageSharp.ColorThief();
            var mainColor = colorThief.GetColor(image, 10, false);
            var theme = new ThemeColorResult(new Vector3(mainColor.Color.R, mainColor.Color.G, mainColor.Color.B), mainColor.IsDark);
            var palette = colorThief.GetPalette(image, 255, 10, false);
            var topColors = palette
                .Where(x => x.IsDark == (isDark ?? mainColor.IsDark))
                .OrderByDescending(x => x.Population)
                .Select(x => new Vector3(x.Color.R, x.Color.G, x.Color.B))
                .Take(count)
                .ToList();
            var paletteResult = new PaletteResult(topColors, mainColor.IsDark, theme);

            return Task.FromResult(paletteResult);
        }

        public static async Task<Dictionary<Vector3, int>> GetPixelColor(BitmapDecoder bitmapDecoder)
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
