using System.Collections.Generic;
using BetterLyrics.Core.Models.Stats;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Controls.Charts;

public sealed partial class HourlyBarChartControl : UserControl
{
    public HourlyBarChartControl()
    {
        this.InitializeComponent();
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
                Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Height = barHeight,
                VerticalAlignment = VerticalAlignment.Bottom
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
                Grid.SetColumnSpan(label, 4); // allow overlapping slightly
                ChartRoot.Children.Add(label);
            }
        }
    }
}
