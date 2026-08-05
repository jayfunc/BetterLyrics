using System.Collections.Generic;
using BetterLyrics.Core.Models.Stats;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls.Charts;

public sealed partial class HourlyBarChartControl : UserControl
{
    private DispatcherTimer _hoverTimer;

    public HourlyBarChartControl()
    {
        this.InitializeComponent();
        _hoverTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(50) };
        _hoverTimer.Tick += HoverTimer_Tick;
    }

    private void HoverTimer_Tick(object sender, object e)
    {
        _hoverTimer.Stop();
        foreach (var child in ChartRoot.Children)
        {
            if (child is Grid container && container.Children.Count > 0 && container.Children[0] is Border b)
            {
                AnimateOpacity(b, 1.0);
            }
        }
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<HourlyActivityItem>), typeof(HourlyBarChartControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<HourlyActivityItem> ItemsSource
    {
        get => (IEnumerable<HourlyActivityItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HourlyBarChartControl control)
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
            Duration = new Duration(System.TimeSpan.FromMilliseconds(200)),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 }
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void DrawChart()
    {
        ChartRoot.Children.Clear();
        ChartRoot.ColumnDefinitions.Clear();
        ChartRoot.RowDefinitions.Clear();

        if (ItemsSource == null) return;

        var items = new List<HourlyActivityItem>(ItemsSource);
        if (items.Count == 0) return;

        ChartRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Bars
        ChartRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Labels

        double maxAvailableHeight = ChartRoot.ActualHeight > 25 ? ChartRoot.ActualHeight - 25 : 100;
        if (maxAvailableHeight <= 0) return;

        var accentBrush = (SolidColorBrush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        Color baseColor = accentBrush.Color;
        
        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.5, 0),
            EndPoint = new Point(0.5, 1)
        };
        gradientBrush.GradientStops.Add(new GradientStop { Color = baseColor, Offset = 0.0 });
        gradientBrush.GradientStops.Add(new GradientStop { Color = ColorHelper.FromArgb(60, baseColor.R, baseColor.G, baseColor.B), Offset = 1.0 });

        for (int i = 0; i < items.Count; i++)
        {
            ChartRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var item = items[i];

            // Bar Container
            var barContainer = new Grid
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(barContainer, i);
            Grid.SetRow(barContainer, 0);

            // Bar Rectangle
            double barHeight = item.HeightPercentage * maxAvailableHeight;
            if (barHeight < 2 && item.Count > 0) barHeight = 2; // minimum visibility

            var bar = new Border
            {
                Background = gradientBrush,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Height = barHeight,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            bar.PointerEntered += (s, ev) => 
            {
                _hoverTimer.Stop();
                foreach (var child in ChartRoot.Children)
                {
                    if (child is Grid container && container.Children.Count > 0 && container.Children[0] is Border b)
                    {
                        AnimateOpacity(b, b == bar ? 1.0 : 0.3);
                    }
                }
            };

            bar.PointerExited += (s, ev) => 
            {
                _hoverTimer.Start();
            };

            var tooltip = new ToolTip
            {
                Content = item,
                ContentTemplate = (DataTemplate)Resources["TooltipTemplate"]
            };
            ToolTipService.SetToolTip(bar, tooltip);

            barContainer.Children.Add(bar);
            ChartRoot.Children.Add(barContainer);

            // X-Axis Label (only show every 4 hours or so to fit, or all if enough space)
            if (i % 4 == 0)
            {
                var label = new TextBlock
                {
                    Text = item.TimeLabel,
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                Grid.SetColumn(label, i);
                Grid.SetRow(label, 1);
                // Grid.SetColumnSpan(label, 4); removed to fix alignment
                ChartRoot.Children.Add(label);
            }
        }
    }
}
