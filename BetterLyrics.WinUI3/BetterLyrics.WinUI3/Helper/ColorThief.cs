using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace BetterLyrics.WinUI3.Helper {
    /// <summary>
    ///     Color map
    /// </summary>
    internal class CMap {
        private readonly List<VBox> vboxes = new List<VBox>();
        private List<QuantizedColor>? palette;

        public void Push(VBox box) {
            palette = null;
            vboxes.Add(box);
        }

        public List<QuantizedColor> GeneratePalette() {
            if (palette == null) {
                palette = (from vBox in vboxes
                           let rgb = vBox.Avg(false)
                           let color = FromRgb(rgb[0], rgb[1], rgb[2])
                           select new QuantizedColor(color, vBox.Count(false))).ToList();
            }

            return palette;
        }

        public int Size() {
            return vboxes.Count;
        }

        public int[]? Map(int[] color) {
            foreach (var vbox in vboxes.Where(vbox => vbox.Contains(color))) {
                return vbox.Avg(false);
            }
            return Nearest(color);
        }

        public int[]? Nearest(int[] color) {
            var d1 = double.MaxValue;
            int[]? pColor = null;

            foreach (var t in vboxes) {
                var vbColor = t.Avg(false);
                var d2 = Math.Sqrt(Math.Pow(color[0] - vbColor[0], 2)
                                   + Math.Pow(color[1] - vbColor[1], 2)
                                   + Math.Pow(color[2] - vbColor[2], 2));
                if (d2 < d1) {
                    d1 = d2;
                    pColor = vbColor;
                }
            }
            return pColor;
        }

        public VBox FindColor(double targetLuma, double minLuma, double maxLuma, double targetSaturation, double minSaturation, double maxSaturation) {
            VBox? max = null;
            double maxValue = 0;
            var highestPopulation = vboxes.Select(p => p.Count(false)).Max();

            foreach (var swatch in vboxes) {
                var avg = swatch.Avg(false);
                var hsl = FromRgb(avg[0], avg[1], avg[2]).ToHsl();
                var sat = hsl.S;
                var luma = hsl.L;

                if (sat >= minSaturation && sat <= maxSaturation &&
                   luma >= minLuma && luma <= maxLuma) {
                    var thisValue = Mmcq.CreateComparisonValue(sat, targetSaturation, luma, targetLuma,
                        swatch.Count(false), highestPopulation);

                    if (max == null || thisValue > maxValue) {
                        max = swatch;
                        maxValue = thisValue;
                    }
                }
            }

            return max;
        }

        public Color FromRgb(int red, int green, int blue) {
            var color = new Color {
                A = 255,
                R = (byte)red,
                G = (byte)green,
                B = (byte)blue
            };

            return color;
        }
    }

    /// <summary>
    ///     Defines a color in RGB space.
    /// </summary>
    public struct Color {
        /// <summary>
        ///     Get or Set the Alpha component value for sRGB.
        /// </summary>
        public byte A;

        /// <summary>
        ///     Get or Set the Blue component value for sRGB.
        /// </summary>
        public byte B;

        /// <summary>
        ///     Get or Set the Green component value for sRGB.
        /// </summary>
        public byte G;

        /// <summary>
        ///     Get or Set the Red component value for sRGB.
        /// </summary>
        public byte R;

        /// <summary>
        ///     Get HSL color.
        /// </summary>
        /// <returns></returns>
        public HslColor ToHsl() {
            const double toDouble = 1.0 / 255;
            var r = toDouble * R;
            var g = toDouble * G;
            var b = toDouble * B;
            var max = Math.Max(Math.Max(r, g), b);
            var min = Math.Min(Math.Min(r, g), b);
            var chroma = max - min;
            double h1;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (chroma == 0) {
                h1 = 0;
            } else if (max == r) {
                h1 = (g - b) / chroma % 6;
            } else if (max == g) {
                h1 = 2 + (b - r) / chroma;
            } else //if (max == b)
              {
                h1 = 4 + (r - g) / chroma;
            }

            var lightness = 0.5 * (max - min);
            var saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs(2 * lightness - 1));
            HslColor ret;
            ret.H = 60 * h1;
            ret.S = saturation;
            ret.L = lightness;
            ret.A = toDouble * A;
            return ret;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        public string ToHexString() {
            return "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
        }

        public string ToHexAlphaString() {
            return "#" + A.ToString("X2") + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
        }

        public override string ToString() {
            if (A == 255) {
                return ToHexString();
            }

            return ToHexAlphaString();
        }
    }

    /// <summary>
    ///     Defines a color in Hue/Saturation/Lightness (HSL) space.
    /// </summary>
    public struct HslColor {
        /// <summary>
        ///     The Alpha/opacity in 0..1 range.
        /// </summary>
        public double A;

        /// <summary>
        ///     The Hue in 0..360 range.
        /// </summary>
        public double H;

        /// <summary>
        ///     The Lightness in 0..1 range.
        /// </summary>
        public double L;

        /// <summary>
        ///     The Saturation in 0..1 range.
        /// </summary>
        public double S;
    }

    internal static class Mmcq {
        public const int Sigbits = 5;
        public const int Rshift = 8 - Sigbits;
        public const int Mult = 1 << Rshift;
        public const int Histosize = 1 << (3 * Sigbits);
        public const int VboxLength = 1 << Sigbits;
        public const double FractByPopulation = 0.75;
        public const int MaxIterations = 1000;
        public const double WeightSaturation = 3d;
        public const double WeightLuma = 6d;
        public const double WeightPopulation = 1d;
        private static readonly VBoxComparer ComparatorProduct = new VBoxComparer();
        private static readonly VBoxCountComparer ComparatorCount = new VBoxCountComparer();

        public static int GetColorIndex(int r, int g, int b) {
            return (r << (2 * Sigbits)) + (g << Sigbits) + b;
        }

        /// <summary>
        ///     Gets the histo.
        /// </summary>
        /// <param name="pixels">The pixels.</param>
        /// <returns>Histo (1-d array, giving the number of pixels in each quantized region of color space), or null on error.</returns>
        private static int[] GetHisto(IEnumerable<byte[]> pixels) {
            var histo = new int[Histosize];

            foreach (var pixel in pixels) {
                var rval = pixel[0] >> Rshift;
                var gval = pixel[1] >> Rshift;
                var bval = pixel[2] >> Rshift;
                var index = GetColorIndex(rval, gval, bval);
                histo[index]++;
            }
            return histo;
        }

        private static VBox VboxFromPixels(IList<byte[]> pixels, int[] histo) {
            int rmin = 1000000, rmax = 0;
            int gmin = 1000000, gmax = 0;
            int bmin = 1000000, bmax = 0;

            // find min/max
            var numPixels = pixels.Count;
            for (var i = 0; i < numPixels; i++) {
                var pixel = pixels[i];
                var rval = pixel[0] >> Rshift;
                var gval = pixel[1] >> Rshift;
                var bval = pixel[2] >> Rshift;

                if (rval < rmin) {
                    rmin = rval;
                } else if (rval > rmax) {
                    rmax = rval;
                }

                if (gval < gmin) {
                    gmin = gval;
                } else if (gval > gmax) {
                    gmax = gval;
                }

                if (bval < bmin) {
                    bmin = bval;
                } else if (bval > bmax) {
                    bmax = bval;
                }
            }

            return new VBox(rmin, rmax, gmin, gmax, bmin, bmax, histo);
        }

        private static VBox[] DoCut(char color, VBox vbox, IList<int> partialsum, IList<int> lookaheadsum, int total) {
            int vboxDim1;
            int vboxDim2;

            switch (color) {
                case 'r':
                    vboxDim1 = vbox.R1;
                    vboxDim2 = vbox.R2;
                    break;
                case 'g':
                    vboxDim1 = vbox.G1;
                    vboxDim2 = vbox.G2;
                    break;
                default:
                    vboxDim1 = vbox.B1;
                    vboxDim2 = vbox.B2;
                    break;
            }

            for (var i = vboxDim1; i <= vboxDim2; i++) {
                if (partialsum[i] > total / 2) {
                    var vbox1 = vbox.Clone();
                    var vbox2 = vbox.Clone();

                    var left = i - vboxDim1;
                    var right = vboxDim2 - i;

                    var d2 = left <= right
                        ? Math.Min(vboxDim2 - 1, Math.Abs(i + right / 2))
                        : Math.Max(vboxDim1, Math.Abs(Convert.ToInt32(i - 1 - left / 2.0)));

                    // avoid 0-count boxes
                    while (d2 < 0 || partialsum[d2] <= 0) {
                        d2++;
                    }
                    var count2 = lookaheadsum[d2];
                    while (count2 == 0 && d2 > 0 && partialsum[d2 - 1] > 0) {
                        count2 = lookaheadsum[--d2];
                    }

                    // set dimensions
                    switch (color) {
                        case 'r':
                            vbox1.R2 = d2;
                            vbox2.R1 = d2 + 1;
                            break;
                        case 'g':
                            vbox1.G2 = d2;
                            vbox2.G1 = d2 + 1;
                            break;
                        default:
                            vbox1.B2 = d2;
                            vbox2.B1 = d2 + 1;
                            break;
                    }

                    return new[] { vbox1, vbox2 };
                }
            }

            throw new Exception("VBox can't be cut");
        }

        private static VBox?[]? MedianCutApply(IList<int> histo, VBox vbox) {
            if (vbox.Count(false) == 0) {
                return null;
            }
            if (vbox.Count(false) == 1) {
                return [vbox.Clone(), null];
            }

            // only one pixel, no split

            var rw = vbox.R2 - vbox.R1 + 1;
            var gw = vbox.G2 - vbox.G1 + 1;
            var bw = vbox.B2 - vbox.B1 + 1;
            var maxw = Math.Max(Math.Max(rw, gw), bw);

            // Find the partial sum arrays along the selected axis.
            var total = 0;
            var partialsum = new int[VboxLength];
            // -1 = not set / 0 = 0
            for (var l = 0; l < partialsum.Length; l++) {
                partialsum[l] = -1;
            }

            // -1 = not set / 0 = 0
            var lookaheadsum = new int[VboxLength];
            for (var l = 0; l < lookaheadsum.Length; l++) {
                lookaheadsum[l] = -1;
            }

            int i, j, k, sum, index;

            if (maxw == rw) {
                for (i = vbox.R1; i <= vbox.R2; i++) {
                    sum = 0;
                    for (j = vbox.G1; j <= vbox.G2; j++) {
                        for (k = vbox.B1; k <= vbox.B2; k++) {
                            index = GetColorIndex(i, j, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else if (maxw == gw) {
                for (i = vbox.G1; i <= vbox.G2; i++) {
                    sum = 0;
                    for (j = vbox.R1; j <= vbox.R2; j++) {
                        for (k = vbox.B1; k <= vbox.B2; k++) {
                            index = GetColorIndex(j, i, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else /* maxw == bw */
              {
                for (i = vbox.B1; i <= vbox.B2; i++) {
                    sum = 0;
                    for (j = vbox.R1; j <= vbox.R2; j++) {
                        for (k = vbox.G1; k <= vbox.G2; k++) {
                            index = GetColorIndex(j, k, i);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }

            for (i = 0; i < VboxLength; i++) {
                if (partialsum[i] != -1) {
                    lookaheadsum[i] = total - partialsum[i];
                }
            }

            // determine the cut planes
            return maxw == rw ? DoCut('r', vbox, partialsum, lookaheadsum, total) : maxw == gw
                    ? DoCut('g', vbox, partialsum, lookaheadsum, total) : DoCut('b', vbox, partialsum, lookaheadsum, total);
        }

        /// <summary>
        ///     Inner function to do the iteration.
        /// </summary>
        /// <param name="lh">The lh.</param>
        /// <param name="comparator">The comparator.</param>
        /// <param name="target">The target.</param>
        /// <param name="histo">The histo.</param>
        /// <exception cref="System.Exception">vbox1 not defined; shouldn't happen!</exception>
        private static void Iter(List<VBox> lh, IComparer<VBox> comparator, int target, IList<int> histo) {
            var ncolors = 1;
            var niters = 0;

            while (niters < MaxIterations) {
                var vbox = lh[lh.Count - 1];
                if (vbox.Count(false) == 0) {
                    lh.Sort(comparator);
                    niters++;
                    continue;
                }

                lh.RemoveAt(lh.Count - 1);

                // do the cut
                var vboxes = MedianCutApply(histo, vbox);
                var vbox1 = vboxes?[0];
                var vbox2 = vboxes?[1];

                if (vbox1 == null) {
                    throw new Exception(
                        "vbox1 not defined; shouldn't happen!");
                }

                lh.Add(vbox1);
                if (vbox2 != null) {
                    lh.Add(vbox2);
                    ncolors++;
                }
                lh.Sort(comparator);

                if (ncolors >= target) {
                    return;
                }
                if (niters++ > MaxIterations) {
                    return;
                }
            }
        }

        public static CMap Quantize(byte[][] pixels, int maxcolors) {
            // short-circuit
            if (pixels.Length == 0 || maxcolors < 2 || maxcolors > 256) {
                return null;
            }

            var histo = GetHisto(pixels);

            // get the beginning vbox from the colors
            var vbox = VboxFromPixels(pixels, histo);
            var pq = new List<VBox> { vbox };

            // Round up to have the same behaviour as in JavaScript
            var target = (int)Math.Ceiling(FractByPopulation * maxcolors);

            // first set of colors, sorted by population
            Iter(pq, ComparatorCount, target, histo);

            // Re-sort by the product of pixel occupancy times the size in color
            // space.
            pq.Sort(ComparatorProduct);

            // next set - generate the median cuts using the (npix * vol) sorting.
            Iter(pq, ComparatorProduct, maxcolors - pq.Count, histo);

            // Reverse to put the highest elements first into the color map
            pq.Reverse();

            // calculate the actual colors
            var cmap = new CMap();
            foreach (var vb in pq) {
                cmap.Push(vb);
            }

            return cmap;
        }

        public static double CreateComparisonValue(double saturation, double targetSaturation, double luma, double targetLuma, int population, int highestPopulation) {
            return WeightedMean(InvertDiff(saturation, targetSaturation), WeightSaturation,
                InvertDiff(luma, targetLuma), WeightLuma,
                population / (double)highestPopulation, WeightPopulation);
        }

        private static double WeightedMean(params double[] values) {
            double sum = 0;
            double sumWeight = 0;

            for (var i = 0; i < values.Length; i += 2) {
                var value = values[i];
                var weight = values[i + 1];

                sum += value * weight;
                sumWeight += weight;
            }

            return sum / sumWeight;
        }

        private static double InvertDiff(double value, double targetValue) {
            return 1 - Math.Abs(value - targetValue);
        }
    }

    public class QuantizedColor {
        public QuantizedColor(Color color, int population) {
            Color = color;
            Population = population;
            IsDark = CalculateYiqLuma(color) < 128;
        }

        public Color Color { get; private set; }
        public int Population { get; private set; }
        public bool IsDark { get; private set; }

        public int CalculateYiqLuma(Color color) {
            return Convert.ToInt32(Math.Round((299 * color.R + 587 * color.G + 114 * color.B) / 1000f));
        }
    }

    /// <summary>
    ///     3D color space box.
    /// </summary>
    internal class VBox {
        private readonly int[] histo;
        private int[] avg;
        public int B1;
        public int B2;
        private int? count;
        public int G1;
        public int G2;
        public int R1;
        public int R2;
        private int? volume;

        public VBox(int r1, int r2, int g1, int g2, int b1, int b2, int[] histo) {
            R1 = r1;
            R2 = r2;
            G1 = g1;
            G2 = g2;
            B1 = b1;
            B2 = b2;

            this.histo = histo;
        }

        public int Volume(bool force) {
            if (volume == null || force) {
                volume = (R2 - R1 + 1) * (G2 - G1 + 1) * (B2 - B1 + 1);
            }

            return volume.Value;
        }

        public int Count(bool force) {
            if (count == null || force) {
                var npix = 0;
                int i;

                for (i = R1; i <= R2; i++) {
                    int j;
                    for (j = G1; j <= G2; j++) {
                        int k;
                        for (k = B1; k <= B2; k++) {
                            var index = Mmcq.GetColorIndex(i, j, k);
                            npix += histo[index];
                        }
                    }
                }

                count = npix;
            }

            return count.Value;
        }

        public VBox Clone() {
            return new VBox(R1, R2, G1, G2, B1, B2, histo);
        }

        public int[] Avg(bool force) {
            if (avg == null || force) {
                var ntot = 0;

                var rsum = 0;
                var gsum = 0;
                var bsum = 0;

                int i;

                for (i = R1; i <= R2; i++) {
                    int j;
                    for (j = G1; j <= G2; j++) {
                        int k;
                        for (k = B1; k <= B2; k++) {
                            var histoindex = Mmcq.GetColorIndex(i, j, k);
                            var hval = histo[histoindex];
                            ntot += hval;
                            rsum += Convert.ToInt32((hval * (i + 0.5) * Mmcq.Mult));
                            gsum += Convert.ToInt32((hval * (j + 0.5) * Mmcq.Mult));
                            bsum += Convert.ToInt32((hval * (k + 0.5) * Mmcq.Mult));
                        }
                    }
                }

                if (ntot > 0) {
                    avg = new[]
                    {
                        Math.Abs(rsum / ntot), Math.Abs(gsum / ntot),
                        Math.Abs(bsum / ntot)
                    };
                } else {
                    avg = new[]
                    {
                        Math.Abs(Mmcq.Mult * (R1 + R2 + 1) / 2),
                        Math.Abs(Mmcq.Mult * (G1 + G2 + 1) / 2),
                        Math.Abs(Mmcq.Mult * (B1 + B2 + 1) / 2)
                    };
                }
            }

            return avg;
        }

        public bool Contains(int[] pixel) {
            var rval = pixel[0] >> Mmcq.Rshift;
            var gval = pixel[1] >> Mmcq.Rshift;
            var bval = pixel[2] >> Mmcq.Rshift;

            return rval >= R1 && rval <= R2 && gval >= G1 && gval <= G2 && bval >= B1 && bval <= B2;
        }
    }

    internal class VBoxCountComparer : IComparer<VBox> {
        public int Compare(VBox x, VBox y) {
            var a = x.Count(false);
            var b = y.Count(false);
            return a < b ? -1 : (a > b ? 1 : 0);
        }
    }

    internal class VBoxComparer : IComparer<VBox> {
        public int Compare(VBox x, VBox y) {
            var aCount = x.Count(false);
            var bCount = y.Count(false);
            var aVolume = x.Volume(false);
            var bVolume = y.Volume(false);

            // Otherwise sort by products
            var a = aCount * aVolume;
            var b = bCount * bVolume;
            return a < b ? -1 : (a > b ? 1 : 0);
        }
    }

    public class ColorThief {
        public const int DefaultColorCount = 5;
        public const int DefaultQuality = 10;
        public const bool DefaultIgnoreWhite = true;
        public const int ColorDepth = 4;

        private CMap GetColorMap(byte[][] pixelArray, int colorCount) {
            // Send array to quantize function which clusters values using median
            // cut algorithm

            if (colorCount > 0) {
                --colorCount;
            }

            var cmap = Mmcq.Quantize(pixelArray, colorCount);
            return cmap;
        }

        private byte[][] ConvertPixels(byte[] pixels, int pixelCount, int quality, bool ignoreWhite) {


            var expectedDataLength = pixelCount * ColorDepth;
            if (expectedDataLength != pixels.Length) {
                throw new ArgumentException("(expectedDataLength = "
                                            + expectedDataLength + ") != (pixels.length = "
                                            + pixels.Length + ")");
            }

            // Store the RGB values in an array format suitable for quantize
            // function

            // numRegardedPixels must be rounded up to avoid an
            // ArrayIndexOutOfBoundsException if all pixels are good.

            var numRegardedPixels = (pixelCount + quality - 1) / quality;

            var numUsedPixels = 0;
            var pixelArray = new byte[numRegardedPixels][];

            for (var i = 0; i < pixelCount; i += quality) {
                var offset = i * ColorDepth;
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];

                // If pixel is mostly opaque and not white
                if (a >= 125 && !(ignoreWhite && r > 250 && g > 250 && b > 250)) {
                    pixelArray[numUsedPixels] = new[] { r, g, b };
                    numUsedPixels++;
                }
            }

            // Remove unused pixels from the array
            var copy = new byte[numUsedPixels][];
            Array.Copy(pixelArray, copy, numUsedPixels);
            return copy;
        }

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
        public async Task<QuantizedColor> GetColor(BitmapDecoder sourceImage, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite) {
            var palette = await GetPalette(sourceImage, 3, quality, ignoreWhite);

            var dominantColor = new QuantizedColor(new Color {
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
        public async Task<List<QuantizedColor>> GetPalette(BitmapDecoder sourceImage, int colorCount = DefaultColorCount, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite) {
            var pixelArray = await GetPixelsFast(sourceImage, quality, ignoreWhite);
            var cmap = GetColorMap(pixelArray, colorCount);
            if (cmap != null) {
                var colors = cmap.GeneratePalette();
                return colors;
            }
            return new List<QuantizedColor>();
        }

        private async Task<byte[]> GetIntFromPixel(BitmapDecoder decoder) {
            var pixelsData = await decoder.GetPixelDataAsync();
            var pixels = pixelsData.DetachPixelData();
            return pixels;
        }

        private async Task<byte[][]> GetPixelsFast(BitmapDecoder sourceImage, int quality, bool ignoreWhite) {
            if (quality < 1) {
                quality = DefaultQuality;
            }

            var pixels = await GetIntFromPixel(sourceImage);
            var pixelCount = sourceImage.PixelWidth * sourceImage.PixelHeight;

            return ConvertPixels(pixels, Convert.ToInt32(pixelCount), quality, ignoreWhite);
        }
    }
}
