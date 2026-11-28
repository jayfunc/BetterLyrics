using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Controls;

/// <summary>
/// 封面改由 Win2D 绘制后，容器在 XAML 里没有内容，Auto 行 / 列会量出 0 而塌陷。
/// 该占位元素按封面原始尺寸报告等比缩放后的期望尺寸，还原旧版 Image 在 Auto 单元格里的测量结果。
/// </summary>
public sealed class AlbumArtMeasurePlaceholder : FrameworkElement
{
    public static readonly DependencyProperty NaturalSizeProperty =
        DependencyProperty.Register(nameof(NaturalSize), typeof(Size), typeof(AlbumArtMeasurePlaceholder),
            new PropertyMetadata(new Size(), OnNaturalSizeChanged));

    public Size NaturalSize
    {
        get => (Size)GetValue(NaturalSizeProperty);
        set => SetValue(NaturalSizeProperty, value);
    }

    private static void OnNaturalSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AlbumArtMeasurePlaceholder)d).InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var natural = NaturalSize;
        if (natural.Width <= 0 || natural.Height <= 0) return new Size();

        // 与 Image.Stretch=Uniform 一致：有限轴上取最小的缩放比，两个方向都可无限伸展时使用原始尺寸
        var scale = double.PositiveInfinity;
        if (!double.IsInfinity(availableSize.Width)) scale = Math.Min(scale, availableSize.Width / natural.Width);
        if (!double.IsInfinity(availableSize.Height)) scale = Math.Min(scale, availableSize.Height / natural.Height);
        if (double.IsInfinity(scale)) scale = 1;

        return new Size(natural.Width * scale, natural.Height * scale);
    }
}
