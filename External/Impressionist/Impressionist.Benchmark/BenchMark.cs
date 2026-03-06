using BenchmarkDotNet.Attributes;
using Impressionist.Implementations;
using System.Drawing;
using System.Numerics;
#pragma warning disable CA1416 // 验证平台兼容性

namespace Impressionist.Benchmark
{
    [MemoryDiagnoser]
    public class BenchMark
    {
        public List<string> fileName = new List<string>()
        {
        };
        public List<Dictionary<Vector3, int>> imageData = new List<Dictionary<Vector3, int>>();
        [Benchmark]
        public async Task GetPaletteOctTree()
        {
            foreach (var item in imageData)
            {
                var result = await PaletteGenerators.OctTreePaletteGenerator.CreatePalette(item, 4);
            }
        }
        [Benchmark]
        public async Task GetPaletteKMeansPP()
        {
            foreach (var item in imageData)
            {
                var result = await PaletteGenerators.KMeansPaletteGenerator.CreatePalette(item, 4, useKMeansPP: true);
            }
        }
        [Benchmark]
        public async Task GetPaletteKMeans()
        {
            foreach (var item in imageData)
            {
                var result = await PaletteGenerators.KMeansPaletteGenerator.CreatePalette(item, 4, useKMeansPP: false);
            }
        }
        [Benchmark]
        public async Task GetPaletteKMeansPPToLab()
        {
            foreach (var item in imageData)
            {
                var result = await PaletteGenerators.KMeansPaletteGenerator.CreatePalette(item, 4, useKMeansPP: true, toLab: true);
            }
        }
        [Benchmark]
        public async Task GetPaletteKMeansToLab()
        {
            foreach (var item in imageData)
            {
                var result = await PaletteGenerators.KMeansPaletteGenerator.CreatePalette(item, 4, useKMeansPP: false, toLab: true);
            }
        }
        [GlobalSetup]
        public void Setup()
        {
            foreach (var item in fileName)
            {
                using var originalImage = new Bitmap(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "/Pictures/", item));
                var result = GetColor(originalImage);
                imageData.Add(result);
            }
        }
        Dictionary<Vector3, int> GetColor(Bitmap bmp)
        {
            var result = new Dictionary<Vector3, int>();
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var clr = bmp.GetPixel(x, y);
                    if (clr.A == 0)
                    {
                        continue;
                    }
                    var vec = new Vector3(clr.R, clr.G, clr.B);
                    if (result.ContainsKey(vec))
                    {
                        result[vec]++;
                    }
                    else
                    {
                        result[vec] = 1;
                    }
                }
            }
            return result;
        }
    }
}
