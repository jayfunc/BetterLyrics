using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ColorThiefDotNet
{
    public partial class ColorThief
    {
        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors and return the base color from the largest cluster.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        public async Task<QuantizedColor> GetColor(BitmapDecoder sourceImage, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite)
        {
            var palette = await GetPalette(sourceImage, 3, quality, ignoreWhite);

            var dominantColor = new QuantizedColor(new Color
            {
                A = Convert.ToByte(palette.Average(a => a.Color.A)),
                R = Convert.ToByte(palette.Average(a => a.Color.R)),
                G = Convert.ToByte(palette.Average(a => a.Color.G)),
                B = Convert.ToByte(palette.Average(a => a.Color.B))
            }, Convert.ToInt32(palette.Average(a => a.Population)));

            return dominantColor;
        }

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="colorCount">The color count.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        /// <code>true</code>
        public async Task<List<QuantizedColor>> GetPalette(BitmapDecoder sourceImage, int colorCount = DefaultColorCount, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite)
        {
            var pixelArray = await GetPixelsFast(sourceImage, quality, ignoreWhite);
            var cmap = GetColorMap(pixelArray, colorCount);
            if(cmap != null)
            {
                var colors = cmap.GeneratePalette();
                return colors;
            }
            return new List<QuantizedColor>();
        }

        private async Task<byte[]> GetIntFromPixel(BitmapDecoder decoder)
        {
            var pixelsData = await decoder.GetPixelDataAsync();
            var pixels = pixelsData.DetachPixelData();
            return pixels;
        }

        private async Task<byte[][]> GetPixelsFast(BitmapDecoder sourceImage, int quality, bool ignoreWhite)
        {
            if(quality < 1)
            {
                quality = DefaultQuality;
            }

            var pixels = await GetIntFromPixel(sourceImage);
            var pixelCount = sourceImage.PixelWidth*sourceImage.PixelHeight;

            return ConvertPixels(pixels, Convert.ToInt32(pixelCount), quality, ignoreWhite);
        }
    }
}