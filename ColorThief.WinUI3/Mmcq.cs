using System;
using System.Collections.Generic;

namespace ColorThiefDotNet
{
    internal static class Mmcq
    {
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

        public static int GetColorIndex(int r, int g, int b)
        {
            return (r << (2 * Sigbits)) + (g << Sigbits) + b;
        }

        /// <summary>
        ///     Gets the histo.
        /// </summary>
        /// <param name="pixels">The pixels.</param>
        /// <returns>Histo (1-d array, giving the number of pixels in each quantized region of color space), or null on error.</returns>
        private static int[] GetHisto(IEnumerable<byte[]> pixels)
        {
            var histo = new int[Histosize];

            foreach(var pixel in pixels)
            {
                var rval = pixel[0] >> Rshift;
                var gval = pixel[1] >> Rshift;
                var bval = pixel[2] >> Rshift;
                var index = GetColorIndex(rval, gval, bval);
                histo[index]++;
            }
            return histo;
        }

        private static VBox VboxFromPixels(IList<byte[]> pixels, int[] histo)
        {
            int rmin = 1000000, rmax = 0;
            int gmin = 1000000, gmax = 0;
            int bmin = 1000000, bmax = 0;

            // find min/max
            var numPixels = pixels.Count;
            for(var i = 0; i < numPixels; i++)
            {
                var pixel = pixels[i];
                var rval = pixel[0] >> Rshift;
                var gval = pixel[1] >> Rshift;
                var bval = pixel[2] >> Rshift;

                if(rval < rmin)
                {
                    rmin = rval;
                }
                else if(rval > rmax)
                {
                    rmax = rval;
                }

                if(gval < gmin)
                {
                    gmin = gval;
                }
                else if(gval > gmax)
                {
                    gmax = gval;
                }

                if(bval < bmin)
                {
                    bmin = bval;
                }
                else if(bval > bmax)
                {
                    bmax = bval;
                }
            }

            return new VBox(rmin, rmax, gmin, gmax, bmin, bmax, histo);
        }

        private static VBox[] DoCut(char color, VBox vbox, IList<int> partialsum, IList<int> lookaheadsum, int total)
        {
            int vboxDim1;
            int vboxDim2;

            switch(color)
            {
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

            for(var i = vboxDim1; i <= vboxDim2; i++)
            {
                if(partialsum[i] > total / 2)
                {
                    var vbox1 = vbox.Clone();
                    var vbox2 = vbox.Clone();

                    var left = i - vboxDim1;
                    var right = vboxDim2 - i;

                    var d2 = left <= right
                        ? Math.Min(vboxDim2 - 1, Math.Abs(i + right / 2))
                        : Math.Max(vboxDim1, Math.Abs(Convert.ToInt32(i - 1 - left / 2.0)));

                    // avoid 0-count boxes
                    while(d2 < 0 || partialsum[d2] <= 0)
                    {
                        d2++;
                    }
                    var count2 = lookaheadsum[d2];
                    while(count2 == 0 && d2 > 0 && partialsum[d2 - 1] > 0)
                    {
                        count2 = lookaheadsum[--d2];
                    }

                    // set dimensions
                    switch(color)
                    {
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

                    return new[] {vbox1, vbox2};
                }
            }

            throw new Exception("VBox can't be cut");
        }

        private static VBox[] MedianCutApply(IList<int> histo, VBox vbox)
        {
            if(vbox.Count(false) == 0)
            {
                return null;
            }
            if(vbox.Count(false) == 1)
            {
                return new[] {vbox.Clone(), null};
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
            for(var l = 0; l < partialsum.Length; l++)
            {
                partialsum[l] = -1;
            }

            // -1 = not set / 0 = 0
            var lookaheadsum = new int[VboxLength];
            for(var l = 0; l < lookaheadsum.Length; l++)
            {
                lookaheadsum[l] = -1;
            }

            int i, j, k, sum, index;

            if(maxw == rw)
            {
                for(i = vbox.R1; i <= vbox.R2; i++)
                {
                    sum = 0;
                    for(j = vbox.G1; j <= vbox.G2; j++)
                    {
                        for(k = vbox.B1; k <= vbox.B2; k++)
                        {
                            index = GetColorIndex(i, j, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }
            else if(maxw == gw)
            {
                for(i = vbox.G1; i <= vbox.G2; i++)
                {
                    sum = 0;
                    for(j = vbox.R1; j <= vbox.R2; j++)
                    {
                        for(k = vbox.B1; k <= vbox.B2; k++)
                        {
                            index = GetColorIndex(j, i, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }
            else /* maxw == bw */
            {
                for(i = vbox.B1; i <= vbox.B2; i++)
                {
                    sum = 0;
                    for(j = vbox.R1; j <= vbox.R2; j++)
                    {
                        for(k = vbox.G1; k <= vbox.G2; k++)
                        {
                            index = GetColorIndex(j, k, i);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }

            for(i = 0; i < VboxLength; i++)
            {
                if(partialsum[i] != -1)
                {
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
        private static void Iter(List<VBox> lh, IComparer<VBox> comparator, int target, IList<int> histo)
        {
            var ncolors = 1;
            var niters = 0;

            while(niters < MaxIterations)
            {
                var vbox = lh[lh.Count - 1];
                if(vbox.Count(false) == 0)
                {
                    lh.Sort(comparator);
                    niters++;
                    continue;
                }

                lh.RemoveAt(lh.Count - 1);

                // do the cut
                var vboxes = MedianCutApply(histo, vbox);
                var vbox1 = vboxes[0];
                var vbox2 = vboxes[1];

                if(vbox1 == null)
                {
                    throw new Exception(
                        "vbox1 not defined; shouldn't happen!");
                }

                lh.Add(vbox1);
                if(vbox2 != null)
                {
                    lh.Add(vbox2);
                    ncolors++;
                }
                lh.Sort(comparator);

                if(ncolors >= target)
                {
                    return;
                }
                if(niters++ > MaxIterations)
                {
                    return;
                }
            }
        }

        public static CMap Quantize(byte[][] pixels, int maxcolors)
        {
            // short-circuit
            if(pixels.Length == 0 || maxcolors < 2 || maxcolors > 256)
            {
                return null;
            }

            var histo = GetHisto(pixels);

            // get the beginning vbox from the colors
            var vbox = VboxFromPixels(pixels, histo);
            var pq = new List<VBox> {vbox};

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
            foreach(var vb in pq)
            {
                cmap.Push(vb);
            }

            return cmap;
        }

        public static double CreateComparisonValue(double saturation, double targetSaturation, double luma, double targetLuma, int population, int highestPopulation)
        {
            return WeightedMean(InvertDiff(saturation, targetSaturation), WeightSaturation,
                InvertDiff(luma, targetLuma), WeightLuma,
                population / (double)highestPopulation, WeightPopulation);
        }

        private static double WeightedMean(params double[] values)
        {
            double sum = 0;
            double sumWeight = 0;

            for(var i = 0; i < values.Length; i += 2)
            {
                var value = values[i];
                var weight = values[i + 1];

                sum += value * weight;
                sumWeight += weight;
            }

            return sum / sumWeight;
        }

        private static double InvertDiff(double value, double targetValue)
        {
            return 1 - Math.Abs(value - targetValue);
        }
    }
}