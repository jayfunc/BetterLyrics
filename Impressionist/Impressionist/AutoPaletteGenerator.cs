using Impressionist.Abstractions;
using Impressionist.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Impressionist
{
    public static class AutoPaletteGenerator
    {
        public static async Task<PaletteResult> CreatePalette(Dictionary<Vector3, int> sourceColor, int clusterCount, bool ignoreWhite = false, bool toLab = false, bool useKMeansPP = false)
        {
            var kmeansResult = await PaletteGenerators.KMeansPaletteGenerator.CreatePalette(sourceColor, clusterCount, ignoreWhite, toLab, useKMeansPP);
            var octTreeResult = await PaletteGenerators.OctTreePaletteGenerator.CreatePalette(sourceColor, clusterCount, ignoreWhite);
            var kMeansCentralPoint = Vector3.Zero;
            var vectors = kmeansResult.Palette.Select(t => t.RGBVectorToLABVector()).ToList();
            foreach (var vector in vectors)
            {
                kMeansCentralPoint += vector;
            }
            kMeansCentralPoint /= clusterCount;
            var distances = vectors.Select(t => Vector3.DistanceSquared(t, kMeansCentralPoint)).ToList();
            var avg = distances.Average();
            var sum = distances.Sum(d => Math.Pow(d - avg, 2));
            var kMeansVariance = sum / clusterCount;
            var octTreeCentralPoint = Vector3.Zero;
            vectors = octTreeResult.Palette.Select(t => t.RGBVectorToLABVector()).ToList();
            foreach (var vector in vectors)
            {
                octTreeCentralPoint += vector;
            }
            octTreeCentralPoint /= clusterCount;
            distances = vectors.Select(t => Vector3.DistanceSquared(t, octTreeCentralPoint)).ToList();
            avg = distances.Average();
            sum = distances.Sum(d => Math.Pow(d - avg, 2));
            var octTreeVariance = sum / clusterCount;
            if (kMeansVariance > octTreeVariance)
            {
                return kmeansResult;
            }
            else
            {
                return octTreeResult;
            }
        }
    }
}
