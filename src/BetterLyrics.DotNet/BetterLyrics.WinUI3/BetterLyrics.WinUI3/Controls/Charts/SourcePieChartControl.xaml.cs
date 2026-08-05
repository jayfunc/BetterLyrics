using System;
using System.Collections.Generic;
using BetterLyrics.Core.Models.Stats;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls.Charts;

public class PieLegendItem
{
    public Brush Brush { get; set; }
    public string Text { get; set; }
}

public sealed partial class SourcePieChartControl : UserControl
{
    private DispatcherTimer _hoverTimer;

    public SourcePieChartControl()
    {
        this.InitializeComponent();
        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _hoverTimer.Tick += HoverTimer_Tick;
    }

    private void HoverTimer_Tick(object sender, object e)
    {
        _hoverTimer.Stop();
        foreach (var child in ChartCanvas.Children)
        {
            if (child is UIElement ui) AnimateOpacity(ui, 1.0);
        }
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<PlayerSourceItem>), typeof(SourcePieChartControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<PlayerSourceItem> ItemsSource
    {
        get => (IEnumerable<PlayerSourceItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourcePieChartControl control)
        {
            control.DrawChart();
        }
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }

    private void AnimateOpacity(UIElement element, double toOpacity)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = toOpacity,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 }
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void DrawChart()
    {
        ChartCanvas.Children.Clear();
        var legendItems = new List<PieLegendItem>();

        if (ItemsSource == null)
        {
            LegendItemsControl.ItemsSource = null;
            return;
        }

        var items = new List<PlayerSourceItem>(ItemsSource);
        if (items.Count == 0)
        {
            LegendItemsControl.ItemsSource = null;
            return;
        }

        double width = ChartContainer.ActualWidth;
        double height = ChartContainer.ActualHeight;

        if (width <= 0 || height <= 0) return;

        double radius = Math.Min(width, height) / 2.0 - 10; // 10px padding
        if (radius <= 0) return;

        ChartCanvas.Width = radius * 2;
        ChartCanvas.Height = radius * 2;

        Point center = new Point(radius, radius);
        double currentAngle = -90; // Start at top

        var accentBrush = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        Color baseColor = accentBrush.Color;
        byte minAlpha = 40;

        int validItemCount = 0;
        foreach (var item in items) if (item.Percentage > 0) validItemCount++;

        int drawnCount = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.Percentage <= 0) continue;

            double sweepAngle = item.Percentage * 360;
            
            byte alpha = 255;
            if (validItemCount > 1)
            {
                alpha = (byte)(255 - (drawnCount * (255 - minAlpha) / (validItemCount - 1)));
            }

            Color sliceColor = ColorHelper.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
            Brush brush = new SolidColorBrush(sliceColor);

            if (item.Percentage >= 0.999)
            {
                // Draw a full circle if it's 100%
                var ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = brush
                };
                var tooltip = new ToolTip
                {
                    Content = item,
                    ContentTemplate = (DataTemplate)Resources["TooltipTemplate"]
                };
                ToolTipService.SetToolTip(ellipse, tooltip);

                ellipse.PointerEntered += (s, ev) =>
                {
                    _hoverTimer.Stop();
                    foreach (var child in ChartCanvas.Children)
                    {
                        if (child is UIElement ui) AnimateOpacity(ui, ui == ellipse ? 1.0 : 0.3);
                    }
                };
                ellipse.PointerExited += (s, ev) =>
                {
                    _hoverTimer.Start();
                };

                ChartCanvas.Children.Add(ellipse);
            }
            else
            {
                // Draw pie slice
                double endAngle = currentAngle + sweepAngle;

                double startRad = currentAngle * Math.PI / 180.0;
                double endRad = endAngle * Math.PI / 180.0;

                Point startPoint = new Point(
                    center.X + radius * Math.Cos(startRad),
                    center.Y + radius * Math.Sin(startRad));

                Point endPoint = new Point(
                    center.X + radius * Math.Cos(endRad),
                    center.Y + radius * Math.Sin(endRad));

                var pathFigure = new PathFigure
                {
                    StartPoint = center,
                    IsClosed = true
                };
                pathFigure.Segments.Add(new LineSegment { Point = startPoint });
                pathFigure.Segments.Add(new ArcSegment
                {
                    Point = endPoint,
                    Size = new Size(radius, radius),
                    IsLargeArc = sweepAngle > 180,
                    SweepDirection = SweepDirection.Clockwise
                });

                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                var path = new Path
                {
                    Fill = brush,
                    Data = pathGeometry
                };
                
                // Exploded pie effect for all slices
                double midRad = (currentAngle + sweepAngle / 2) * Math.PI / 180.0;
                path.RenderTransform = new TranslateTransform
                {
                    X = 3 * Math.Cos(midRad),
                    Y = 3 * Math.Sin(midRad)
                };

                var tooltip = new ToolTip
                {
                    Content = item,
                    ContentTemplate = (DataTemplate)Resources["TooltipTemplate"]
                };
                ToolTipService.SetToolTip(path, tooltip);

                path.PointerEntered += (s, ev) =>
                {
                    _hoverTimer.Stop();
                    foreach (var child in ChartCanvas.Children)
                    {
                        if (child is UIElement ui) AnimateOpacity(ui, ui == path ? 1.0 : 0.3);
                    }
                };
                path.PointerExited += (s, ev) =>
                {
                    _hoverTimer.Start();
                };

                ChartCanvas.Children.Add(path);
            }

            currentAngle += sweepAngle;

            legendItems.Add(new PieLegendItem
            {
                Brush = brush,
                Text = item.Name
            });
            
            drawnCount++;
        }

        LegendItemsControl.ItemsSource = legendItems;
    }
}
