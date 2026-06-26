using Impressionist.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Impressionist.Implementations
{
    public static class AutoPaletteGenerator
    {
        public static async Task <PaletteResult> CreatePalette(Dictionary<Vector3, int> sourceColor, int clusterCount, bool ignoreWhite = false, bool toLab = false, bool useKMeansPP = false)
        {
            var theme = KMeansPaletteGenerator.CreateThemeColor(sourceColor,ignoreWhite, toLab);
            var kmeansTask = KMeansPaletteGenerator.CreatePalette(sourceColor, clusterCount, theme,ignoreWhite, toLab, useKMeansPP);
            var octTreeTask = OctTreePaletteGenerator.CreatePalette(sourceColor, clusterCount, theme,ignoreWhite);

            await Task.WhenAll(kmeansTask, octTreeTask);

            var kmeansResult = kmeansTask.Result;
            var octTreeResult = octTreeTask.Result;
            var kMeansDiversity = CalculateSpatialDiversity(kmeansResult.Palette);
            var octTreeDiversity = CalculateSpatialDiversity(octTreeResult.Palette);
            if (kmeansResult.PaletteIsDark)
            {
                return kMeansDiversity >= octTreeDiversity ? kmeansResult : octTreeResult;
            }
            else
            {
                return kMeansDiversity <= octTreeDiversity || octTreeDiversity == 0 ? kmeansResult : octTreeResult;
            }
        }

        private static double CalculateSpatialDiversity(List<Vector3> palette)
        {
            if (palette == null || palette.Count == 0) return 0;

            var labVectors = palette.Select(t => t.RGBVectorToLABVector()).ToList();
            var centroid = Vector3.Zero;
            foreach (var vector in labVectors)
            {
                centroid += vector;
            }
            centroid /= labVectors.Count;

            var sumSquaredDistances = labVectors.Sum(v => Vector3.DistanceSquared(v, centroid));

            return sumSquaredDistances / labVectors.Count;
        }
    }
}