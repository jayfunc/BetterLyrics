using System;
using System.Collections.Generic;
using System.Linq;
using BetterLyrics.Core.Models.Stats;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls.Charts;

public sealed partial class SourceBarChartControl : UserControl
{
    public SourceBarChartControl()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<PlayerSourceItem>), typeof(SourceBarChartControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<PlayerSourceItem> ItemsSource
    {
        get => (IEnumerable<PlayerSourceItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
    
    private double MaxCount { get; set; } = 0;

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourceBarChartControl control)
        {
            if (control.ItemsSource != null)
            {
                var items = control.ItemsSource.Where(x => x.Count > 0).OrderByDescending(x => x.Count).Take(10).ToList();
                if (items.Any())
                {
                    control.MaxCount = items.First().Count;
                }
                else
                {
                    control.MaxCount = 0;
                }
                control.ChartItemsControl.ItemsSource = items;
            }
            else
            {
                control.MaxCount = 0;
                control.ChartItemsControl.ItemsSource = null;
            }
        }
    }

    private void TrackBg_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBarWidth(sender as Grid);
    }
    
    private void TrackBg_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateBarWidth(sender as Grid);
    }

    private void UpdateBarWidth(Grid trackGrid)
    {
        if (trackGrid != null && trackGrid.Children.Count > 0 && trackGrid.Children[0] is Border fillBorder)
        {
            if (trackGrid.Tag is int count && MaxCount > 0)
            {
                double ratio = (double)count / MaxCount;
                double targetWidth = trackGrid.ActualWidth * ratio;
                if (targetWidth < 0) targetWidth = 0;
                if (targetWidth > trackGrid.ActualWidth) targetWidth = trackGrid.ActualWidth;

                fillBorder.Width = targetWidth;
            }
            else if (MaxCount == 0)
            {
                fillBorder.Width = 0;
            }
        }
    }
}
