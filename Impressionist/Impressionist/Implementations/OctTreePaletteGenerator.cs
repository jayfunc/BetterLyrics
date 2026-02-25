using Impressionist.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Impressionist.Implementations
{
    public class OctTreePaletteGenerator :
        IThemeColorGenrator,
        IPaletteGenrator
    {
        private static Task<ThemeColorResult> CreateThemeColorAsync(Dictionary<Vector3, int> sourceColor, bool ignoreWhite = false)
        {
            var quantizer = new PaletteQuantizer();
            var builder = sourceColor.AsEnumerable();
            if (ignoreWhite && sourceColor.Count > 1)
            {
                builder = builder.Where(t => t.Key.X <= 250 || t.Key.Y <= 250 || t.Key.Z <= 250);
            }
            var targetColor = builder.ToDictionary(t => t.Key, t => t.Value);
            foreach (var color in targetColor)
            {
                quantizer.AddColorRange(color.Key, color.Value);
            }
            quantizer.Quantize(1);
            var index = new List<Vector3>() { targetColor.Keys.FirstOrDefault() };
            var result = quantizer.GetThemeResult();
            var colorIsDark = result.RGBVectorLStarIsDark();
            return Task.FromResult(new ThemeColorResult(result, colorIsDark));
        }

        public static async Task<PaletteResult> CreatePaletteAsync(Dictionary<Vector3, int> sourceColor, int clusterCount, bool isDark)
        {
            var quantizer = new PaletteQuantizer();
            var builder = sourceColor.AsEnumerable();
            var colorResult = await CreateThemeColorAsync(sourceColor);
            builder = builder.Where(t => t.Key.RGBVectorLStarIsDark() == isDark);
            var targetColor = builder.ToDictionary(t => t.Key, t => t.Value);
            foreach (var color in targetColor)
            {
                quantizer.AddColorRange(color.Key, color.Value);
            }
            quantizer.Quantize(clusterCount);
            var index = targetColor.Keys.ToList();
            List<Vector3> quantizeResult;
            if (isDark)
            {
                quantizeResult = quantizer.GetPaletteResult(clusterCount);
            }
            else
            {
                quantizeResult = quantizer.GetPaletteResult(clusterCount);
            }
            List<Vector3> result;
            if (quantizeResult.Count < clusterCount)
            {
                var count = quantizeResult.Count;
                result = new List<Vector3>();
                for (int i = 0; i < clusterCount; i++)
                {
                    // You know, it is always hard to fullfill a palette when you have no enough colors. So please forgive me when placing the same color over and over again.
                    result.Add(quantizeResult[i % count]);
                }
            }
            else
            {
                result = quantizeResult;
            }
            return new PaletteResult(result, isDark, colorResult);
        }

        private class PaletteQuantizer
        {
            private readonly Node Root;
            private IDictionary<int, List<Node>> levelNodes;

            public PaletteQuantizer()
            {
                Root = new Node(this);
                levelNodes = new Dictionary<int, List<Node>>();
                for (int i = 0; i < 8; i++)
                {
                    levelNodes[i] = new List<Node>();
                }
            }

            public void AddColor(Vector3 color)
            {
                Root.AddColor(color, 0);
            }

            public void AddColorRange(Vector3 color, int count)
            {
                Root.AddColorRange(color, 0, count);
            }

            public void AddLevelNode(Node node, int level)
            {
                levelNodes[level].Add(node);
            }

            public List<Vector3> GetPaletteResult()
            {
                return Root.GetPaletteResult().Keys.ToList();
            }
            public List<Vector3> GetPaletteResult(int count)
            {
                return Root.GetPaletteResult().OrderByDescending(t => t.Value).Take(count).Select(t => t.Key).ToList();
            }
            public Vector3 GetThemeResult()
            {
                return Root.GetThemeResult();
            }
            public void Quantize(int colorCount)
            {
                var nodesToRemove = levelNodes[7].Count - colorCount;
                int level = 6;
                var toBreak = false;
                while (level >= 0 && nodesToRemove > 0)
                {
                    var leaves = levelNodes[level]
                        .Where(n => n.ChildrenCount - 1 <= nodesToRemove)
                        .OrderBy(n => n.ChildrenCount);
                    foreach (var leaf in leaves)
                    {
                        if (leaf.ChildrenCount > nodesToRemove)
                        {
                            toBreak = true;
                            continue;
                        }
                        nodesToRemove -= (leaf.ChildrenCount - 1);
                        leaf.Merge();
                        if (nodesToRemove <= 0)
                        {
                            break;
                        }
                    }
                    levelNodes.Remove(level + 1);
                    level--;
                    if (toBreak)
                    {
                        break;
                    }
                }
            }
        }

        private class Node
        {
            private readonly PaletteQuantizer parent;
            private Node[] Children = new Node[8];
            private Vector3 Color { get; set; }
            private int Count { get; set; } = 0;

            public int ChildrenCount => Children.Count(c => c != null);

            public Node(PaletteQuantizer parent)
            {
                this.parent = parent;
            }

            public void AddColor(Vector3 color, int level)
            {
                if (level < 8)
                {
                    var index = GetIndex(color, level);
                    if (Children[index] == null)
                    {
                        var newNode = new Node(parent);
                        Children[index] = newNode;
                        parent.AddLevelNode(newNode, level);
                    }
                    Children[index].AddColor(color, level + 1);
                }
                else
                {
                    Color = color;
                    Count++;
                }
            }
            public void AddColorRange(Vector3 color, int level, int count)
            {
                if (level < 8)
                {
                    var index = GetIndex(color, level);
                    if (Children[index] == null)
                    {
                        var newNode = new Node(parent);
                        Children[index] = newNode;
                        parent.AddLevelNode(newNode, level);
                    }
                    Children[index].AddColorRange(color, level + 1, count);
                }
                else
                {
                    Color = color;
                    Count += count;
                }
            }

            public Vector3 GetColor(Vector3 color, int level)
            {
                if (ChildrenCount == 0)
                {
                    return Color;
                }
                else
                {
                    var index = GetIndex(color, level);
                    return Children[index].GetColor(color, level + 1);
                }
            }
            public Vector3 GetThemeResult()
            {
                var paletteResult = GetPaletteResult();
                var sum = new Vector3(0, 0, 0);
                var count = 0;
                foreach (var item in paletteResult)
                {
                    sum += item.Key * item.Value;
                    count += item.Value;
                }
                return sum / count;
            }
            public Dictionary<Vector3, int> GetPaletteResult()
            {
                var result = new Dictionary<Vector3, int>();
                if (!Children.Any(t => t != null)) result[Color] = Count;
                else
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            child.NodeGetResult(result);
                        }
                    }
                }
                return result;
            }
            private void NodeGetResult(Dictionary<Vector3, int> result)
            {
                if (!Children.Any(t => t != null)) result[Color] = Count;
                else
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            child.NodeGetResult(result);
                        }
                    }
                }
            }
            private byte GetIndex(Vector3 color, int level)
            {
                byte ret = 0;
                var mask = Convert.ToByte(0b10000000 >> level);
                if (((byte)color.X & mask) != 0)
                {
                    ret |= 0b100;
                }
                if (((byte)color.Y & mask) != 0)
                {
                    ret |= 0b010;
                }
                if (((byte)color.Z & mask) != 0)
                {
                    ret |= 0b001;
                }
                return ret;
            }

            public void Merge()
            {
                Color = Average(Children.Where(c => c != null).Select(c => new Tuple<Vector3, int>(c.Color, c.Count)));
                Count = Children.Sum(c => c?.Count ?? 0);
                Children = new Node[8];
            }

            private static Vector3 Average(IEnumerable<Tuple<Vector3, int>> colors)
            {
                var totals = colors.Sum(c => c.Item2);
                return new Vector3(
                    x: (int)colors.Sum(c => c.Item1.X * c.Item2) / totals,
                    y: (int)colors.Sum(c => c.Item1.Y * c.Item2) / totals,
                    z: (int)colors.Sum(c => c.Item1.Z * c.Item2) / totals);
            }
        }
    }
}