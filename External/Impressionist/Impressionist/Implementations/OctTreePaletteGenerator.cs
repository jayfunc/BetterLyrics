using Impressionist.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Impressionist.Implementations
{
    public static class OctTreePaletteGenerator
    {
        public static Task<PaletteResult> CreatePalette(Dictionary<Vector3, int> sourceColor, int clusterCount, bool ignoreWhite = false)
        {
            var colorResult = KMeansPaletteGenerator.CreateThemeColor(sourceColor, ignoreWhite, true);
            return CreatePalette(sourceColor, clusterCount, colorResult, ignoreWhite);
        }

        public static Task<PaletteResult> CreatePalette(Dictionary<Vector3, int> sourceColor, int clusterCount, ThemeColorResult colorResult, bool ignoreWhite = false)
        {
            var quantizer = new OctreePaletteQuantizer();
            if (sourceColor.Count == 1)
            {
                ignoreWhite = false;
            }
            var builder = sourceColor.AsEnumerable();
            if (ignoreWhite)
            {
                builder = builder.Where(t => t.Key.X <= 250 || t.Key.Y <= 250 || t.Key.Z <= 250);
            }
            if (colorResult.ColorIsDark)
            {
                builder = builder.Where(t => t.Key.PaletteRGBVectorLStarIsDark());
            }
            else
            {
                builder = builder.Where(t => t.Key.PaletteRGBVectorLStarIsLight());
            }
            var targetColor = builder.ToDictionary(t => t.Key, t => t.Value);
            foreach (var color in targetColor)
            {
                quantizer.AddColor(color.Key, color.Value);
            }
            quantizer.ReduceToColorCount(clusterCount);
            List<Vector3> quantizeResult = quantizer.GetPalette(clusterCount);
            List<Vector3> result;
            if (quantizeResult.Count < clusterCount)
            {
                var count = quantizeResult.Count;
                result = new List<Vector3>();
                if (count > 0)
                {
                    for (int i = 0; i < clusterCount; i++)
                    {
                        result.Add(quantizeResult[i % count]);
                    }
                }
                else
                {
                    for (int i = 0; i < clusterCount; i++) result.Add(Vector3.Zero);
                }
            }
            else
            {
                result = quantizeResult;
            }
            return Task.FromResult(new PaletteResult(result, colorResult.ColorIsDark, colorResult));
        }
        internal sealed class OctreePaletteQuantizer
        {
            private const int MaxColorDepth = 8;

            private readonly OctreeNode _rootNode;
            private readonly List<OctreeNode>[] _nodesByDepth;

            public OctreePaletteQuantizer()
            {
                _rootNode = new OctreeNode(this, null, -1);
                _nodesByDepth = new List<OctreeNode>[MaxColorDepth];

                for (int depth = 0; depth < MaxColorDepth; depth++)
                {
                    _nodesByDepth[depth] = new List<OctreeNode>();
                }
            }

            public void AddColor(Vector3 color)
            {
                AddColor(color, 1);
            }

            public void AddColor(Vector3 color, int sampleCount)
            {
                if (sampleCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(sampleCount));

                if (sampleCount == 0)
                    return;

                byte red = (byte)color.X;
                byte green = (byte)color.Y;
                byte blue = (byte)color.Z;

                _rootNode.AddColor(red, green, blue, 0, sampleCount);
            }

            public void AddColor(byte red, byte green, byte blue)
            {
                _rootNode.AddColor(red, green, blue, 0, 1);
            }

            public void AddColor(byte red, byte green, byte blue, int sampleCount)
            {
                if (sampleCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(sampleCount));

                if (sampleCount == 0)
                    return;

                _rootNode.AddColor(red, green, blue, 0, sampleCount);
            }

            internal void RegisterNodeAtDepth(OctreeNode node, int depth)
            {
                _nodesByDepth[depth].Add(node);
            }

            public List<Vector3> GetPalette()
            {
                if (_rootNode.LeafNodeCount == 0)
                    return new List<Vector3>();

                List<Vector3> palette = new List<Vector3>(_rootNode.LeafNodeCount);
                _rootNode.CollectColors(palette);
                return palette;
            }

            public List<Vector3> GetPalette(int maxColorCount)
            {
                if (maxColorCount <= 0 || _rootNode.LeafNodeCount == 0)
                    return new List<Vector3>();

                List<PaletteColor> paletteColors = new List<PaletteColor>(_rootNode.LeafNodeCount);
                _rootNode.CollectPaletteColors(paletteColors);

                if (paletteColors.Count <= maxColorCount)
                {
                    List<Vector3> palette = new List<Vector3>(paletteColors.Count);

                    for (int i = 0; i < paletteColors.Count; i++)
                    {
                        palette.Add(paletteColors[i].Color);
                    }

                    return palette;
                }

                paletteColors.Sort(ComparePaletteColorsBySampleCountDescending);

                int actualColorCount = Math.Min(maxColorCount, paletteColors.Count);
                List<Vector3> result = new List<Vector3>(actualColorCount);

                for (int i = 0; i < actualColorCount; i++)
                {
                    result.Add(paletteColors[i].Color);
                }

                return result;
            }

            public void ReduceToColorCount(int targetColorCount)
            {
                if (targetColorCount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(targetColorCount));

                int remainingLeafReduction = _rootNode.LeafNodeCount - targetColorCount;

                if (remainingLeafReduction <= 0)
                    return;

                for (int depth = MaxColorDepth - 2; depth >= 0 && remainingLeafReduction > 0; depth--)
                {
                    List<OctreeNode> nodesAtDepth = _nodesByDepth[depth];

                    nodesAtDepth.Sort(CompareMergeCandidates);

                    for (int i = 0; i < nodesAtDepth.Count && remainingLeafReduction > 0; i++)
                    {
                        OctreeNode candidate = nodesAtDepth[i];

                        if (candidate.ChildCount == 0)
                            continue;

                        int leafReduction = candidate.LeafNodeCount - 1;

                        if (leafReduction <= 0)
                            continue;

                        if (leafReduction > remainingLeafReduction)
                            continue;

                        remainingLeafReduction -= leafReduction;
                        candidate.MergeChildrenIntoThisNode();
                    }
                }

                while (_rootNode.LeafNodeCount > targetColorCount)
                {
                    OctreeNode candidate = FindBestMergeCandidate();

                    if (candidate == null)
                        break;

                    candidate.MergeChildrenIntoThisNode();
                }
            }

            private OctreeNode FindBestMergeCandidate()
            {
                OctreeNode bestCandidate = null;
                int bestLeafReduction = int.MaxValue;
                long bestSampleCount = long.MaxValue;

                for (int depth = MaxColorDepth - 2; depth >= 0; depth--)
                {
                    List<OctreeNode> nodesAtDepth = _nodesByDepth[depth];

                    for (int i = 0; i < nodesAtDepth.Count; i++)
                    {
                        OctreeNode candidate = nodesAtDepth[i];

                        if (!candidate.IsAttachedToRoot())
                            continue;

                        if (candidate.ChildCount == 0)
                            continue;

                        int leafReduction = candidate.LeafNodeCount - 1;

                        if (leafReduction <= 0)
                            continue;

                        if (leafReduction < bestLeafReduction ||
                            leafReduction == bestLeafReduction && candidate.SampleCount < bestSampleCount)
                        {
                            bestCandidate = candidate;
                            bestLeafReduction = leafReduction;
                            bestSampleCount = candidate.SampleCount;
                        }
                    }
                }

                return bestCandidate;
            }

            private static int CompareMergeCandidates(OctreeNode left, OctreeNode right)
            {
                int leafCountCompare = left.LeafNodeCount.CompareTo(right.LeafNodeCount);

                if (leafCountCompare != 0)
                    return leafCountCompare;

                return left.SampleCount.CompareTo(right.SampleCount);
            }

            private static int ComparePaletteColorsBySampleCountDescending(PaletteColor left, PaletteColor right)
            {
                int sampleCountCompare = right.SampleCount.CompareTo(left.SampleCount);

                if (sampleCountCompare != 0)
                    return sampleCountCompare;

                int redCompare = left.Color.X.CompareTo(right.Color.X);

                if (redCompare != 0)
                    return redCompare;

                int greenCompare = left.Color.Y.CompareTo(right.Color.Y);

                if (greenCompare != 0)
                    return greenCompare;

                return left.Color.Z.CompareTo(right.Color.Z);
            }

            internal struct PaletteColor
            {
                public readonly Vector3 Color;
                public readonly long SampleCount;

                public PaletteColor(Vector3 color, long sampleCount)
                {
                    Color = color;
                    SampleCount = sampleCount;
                }
            }

            internal sealed class OctreeNode
            {
                private readonly OctreePaletteQuantizer _owner;
                private readonly OctreeNode _parentNode;
                private readonly int _indexInParent;

                private OctreeNode _child0;
                private OctreeNode _child1;
                private OctreeNode _child2;
                private OctreeNode _child3;
                private OctreeNode _child4;
                private OctreeNode _child5;
                private OctreeNode _child6;
                private OctreeNode _child7;

                private int _childCount;
                private int _leafNodeCount;

                private long _sampleCount;
                private long _redSum;
                private long _greenSum;
                private long _blueSum;

                public OctreeNode(OctreePaletteQuantizer owner, OctreeNode parentNode, int indexInParent)
                {
                    _owner = owner;
                    _parentNode = parentNode;
                    _indexInParent = indexInParent;
                }

                public int ChildCount
                {
                    get { return _childCount; }
                }

                public int LeafNodeCount
                {
                    get { return _leafNodeCount; }
                }

                public long SampleCount
                {
                    get { return _sampleCount; }
                }

                private Vector3 AverageColor
                {
                    get
                    {
                        if (_sampleCount == 0)
                            return Vector3.Zero;

                        return new Vector3(
                            (float)_redSum / _sampleCount,
                            (float)_greenSum / _sampleCount,
                            (float)_blueSum / _sampleCount);
                    }
                }

                public void AddColor(byte red, byte green, byte blue, int depth, int sampleCount)
                {
                    _sampleCount += sampleCount;
                    _redSum += (long)red * sampleCount;
                    _greenSum += (long)green * sampleCount;
                    _blueSum += (long)blue * sampleCount;

                    if (depth == MaxColorDepth)
                    {
                        if (_leafNodeCount == 0)
                            _leafNodeCount = 1;

                        return;
                    }

                    int childIndex = GetChildIndex(red, green, blue, depth);
                    OctreeNode childNode = GetChild(childIndex);

                    if (childNode == null)
                    {
                        childNode = new OctreeNode(_owner, this, childIndex);
                        SetChild(childIndex, childNode);
                        _childCount++;

                        _owner.RegisterNodeAtDepth(childNode, depth);
                    }

                    int previousLeafNodeCount = childNode._leafNodeCount;

                    childNode.AddColor(red, green, blue, depth + 1, sampleCount);

                    _leafNodeCount += childNode._leafNodeCount - previousLeafNodeCount;
                }

                public void CollectColors(List<Vector3> result)
                {
                    if (_leafNodeCount == 0)
                        return;

                    if (_childCount == 0)
                    {
                        result.Add(AverageColor);
                        return;
                    }

                    if (_child0 != null) _child0.CollectColors(result);
                    if (_child1 != null) _child1.CollectColors(result);
                    if (_child2 != null) _child2.CollectColors(result);
                    if (_child3 != null) _child3.CollectColors(result);
                    if (_child4 != null) _child4.CollectColors(result);
                    if (_child5 != null) _child5.CollectColors(result);
                    if (_child6 != null) _child6.CollectColors(result);
                    if (_child7 != null) _child7.CollectColors(result);
                }

                public void CollectPaletteColors(List<PaletteColor> result)
                {
                    if (_leafNodeCount == 0)
                        return;

                    if (_childCount == 0)
                    {
                        result.Add(new PaletteColor(AverageColor, _sampleCount));
                        return;
                    }

                    if (_child0 != null) _child0.CollectPaletteColors(result);
                    if (_child1 != null) _child1.CollectPaletteColors(result);
                    if (_child2 != null) _child2.CollectPaletteColors(result);
                    if (_child3 != null) _child3.CollectPaletteColors(result);
                    if (_child4 != null) _child4.CollectPaletteColors(result);
                    if (_child5 != null) _child5.CollectPaletteColors(result);
                    if (_child6 != null) _child6.CollectPaletteColors(result);
                    if (_child7 != null) _child7.CollectPaletteColors(result);
                }

                public void MergeChildrenIntoThisNode()
                {
                    if (_childCount == 0 || _leafNodeCount <= 1)
                        return;

                    int previousLeafNodeCount = _leafNodeCount;

                    _child0 = null;
                    _child1 = null;
                    _child2 = null;
                    _child3 = null;
                    _child4 = null;
                    _child5 = null;
                    _child6 = null;
                    _child7 = null;

                    _childCount = 0;
                    _leafNodeCount = _sampleCount > 0 ? 1 : 0;

                    int leafReduction = previousLeafNodeCount - _leafNodeCount;

                    OctreeNode parentNode = _parentNode;

                    while (parentNode != null)
                    {
                        parentNode._leafNodeCount -= leafReduction;
                        parentNode = parentNode._parentNode;
                    }
                }

                public bool IsAttachedToRoot()
                {
                    OctreeNode currentNode = this;

                    while (currentNode._parentNode != null)
                    {
                        if (!ReferenceEquals(
                            currentNode._parentNode.GetChild(currentNode._indexInParent),
                            currentNode))
                        {
                            return false;
                        }

                        currentNode = currentNode._parentNode;
                    }

                    return true;
                }

                private OctreeNode GetChild(int childIndex)
                {
                    switch (childIndex)
                    {
                        case 0:
                            return _child0;
                        case 1:
                            return _child1;
                        case 2:
                            return _child2;
                        case 3:
                            return _child3;
                        case 4:
                            return _child4;
                        case 5:
                            return _child5;
                        case 6:
                            return _child6;
                        case 7:
                            return _child7;
                        default:
                            return null;
                    }
                }

                private void SetChild(int childIndex, OctreeNode childNode)
                {
                    switch (childIndex)
                    {
                        case 0:
                            _child0 = childNode;
                            break;
                        case 1:
                            _child1 = childNode;
                            break;
                        case 2:
                            _child2 = childNode;
                            break;
                        case 3:
                            _child3 = childNode;
                            break;
                        case 4:
                            _child4 = childNode;
                            break;
                        case 5:
                            _child5 = childNode;
                            break;
                        case 6:
                            _child6 = childNode;
                            break;
                        case 7:
                            _child7 = childNode;
                            break;
                    }
                }

                private static int GetChildIndex(byte red, byte green, byte blue, int depth)
                {
                    int bitShift = 7 - depth;

                    return (((red >> bitShift) & 1) << 2)
                         | (((green >> bitShift) & 1) << 1)
                         | ((blue >> bitShift) & 1);
                }
            }
        }
    }
}