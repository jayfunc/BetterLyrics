using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace BetterLyrics.Avalonia.Extensions; // 确保命名空间与你调用它的地方一致

public static class TextLayoutExtensions
{
    /// <summary>
    /// 将 Avalonia 的 TextLayout 转换为完整的 Geometry 路径，用于实现文本描边等效果。
    /// </summary>
    public static Geometry? BuildGeometry(this TextLayout layout, Point origin)
    {
        if (layout == null) return null;

        var geometryGroup = new GeometryGroup();
        double currentY = origin.Y;

        foreach (var line in layout.TextLines)
        {
            double currentX = origin.X;

            // 处理 TextLayout 的水平对齐偏移量
            // TODO
            // switch (layout)
            // {
            //     case TextAlignment.Center:
            //         currentX += (layout.Width - line.Width) / 2;
            //         break;
            //     case TextAlignment.Right:
            //         currentX += (layout.Width - line.Width);
            //         break;
            // }

            // 遍历当前行的所有文本块 (Run)
            foreach (var run in line.TextRuns)
            {
                // 如果是可绘制的字形集，提取 Geometry
                if (run is ShapedTextRun shapedRun && shapedRun.GlyphRun != null)
                {
                    var runGeometry = shapedRun.GlyphRun.BuildGeometry();
                    
                    // GlyphRun 的几何路径是相对于其基线 (Baseline) 的，需要做平移矩阵变换
                    var transform = Matrix.CreateTranslation(currentX, currentY + line.Baseline);
                    runGeometry.Transform = new MatrixTransform(transform);
                    
                    geometryGroup.Children.Add(runGeometry);
                }
                
                // 推进 X 轴光标到下一个 Run 的起点
                if (run is DrawableTextRun drawableRun)
                {
                    currentX += drawableRun.Size.Width;
                }
            }

            // 推进 Y 轴到下一行
            currentY += line.Height;
        }

        return geometryGroup;
    }
}