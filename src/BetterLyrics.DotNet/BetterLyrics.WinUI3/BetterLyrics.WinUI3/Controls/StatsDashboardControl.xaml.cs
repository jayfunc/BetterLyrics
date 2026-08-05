using System;
using System.Globalization;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StatsDashboardControl : UserControl
{
    private DispatcherTimer _heatmapHoverTimer;
    private VariableSizedWrapGrid _currentWrapGrid;

    public StatsDashboardControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<StatsDashboardControlViewModel>();
        _heatmapHoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _heatmapHoverTimer.Tick += HeatmapHoverTimer_Tick;
    }

    private void HeatmapHoverTimer_Tick(object sender, object e)
    {
        _heatmapHoverTimer.Stop();
        if (_currentWrapGrid != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(_currentWrapGrid); i++)
            {
                var child = VisualTreeHelper.GetChild(_currentWrapGrid, i);
                var border = FindVisualChild<Border>(child);
                if (border != null && border.DataContext is BetterLyrics.Core.Models.HeatmapNode node)
                {
                    AnimateOpacity(border, node.Opacity);
                }
            }
        }
    }

    public StatsDashboardControlViewModel ViewModel => (StatsDashboardControlViewModel)DataContext;

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        var culture = CultureInfo.CurrentUICulture;
        var dtfi = culture.DateTimeFormat;
        HeatmapLabel1.Text = dtfi.GetDayName((DayOfWeek)(((int)dtfi.FirstDayOfWeek + 1) % 7));
        HeatmapLabel3.Text = dtfi.GetDayName((DayOfWeek)(((int)dtfi.FirstDayOfWeek + 3) % 7));
        HeatmapLabel5.Text = dtfi.GetDayName((DayOfWeek)(((int)dtfi.FirstDayOfWeek + 5) % 7));
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

    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void HeatmapNode_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border hoveredBorder)
        {
            _heatmapHoverTimer.Stop();
            var wrapGrid = VisualTreeHelper.GetParent(hoveredBorder);
            while (wrapGrid != null && !(wrapGrid is VariableSizedWrapGrid))
            {
                wrapGrid = VisualTreeHelper.GetParent(wrapGrid);
            }
            if (wrapGrid != null)
            {
                _currentWrapGrid = (VariableSizedWrapGrid)wrapGrid;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(wrapGrid); i++)
                {
                    var child = VisualTreeHelper.GetChild(wrapGrid, i);
                    var border = FindVisualChild<Border>(child);
                    if (border != null && border != hoveredBorder && border.DataContext is BetterLyrics.Core.Models.HeatmapNode node)
                    {
                        AnimateOpacity(border, node.Opacity * 0.3);
                    }
                    else if (border == hoveredBorder && border.DataContext is BetterLyrics.Core.Models.HeatmapNode node2)
                    {
                        AnimateOpacity(border, node2.Opacity);
                    }
                }
            }
        }
    }

    private void HeatmapNode_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _heatmapHoverTimer.Start();
    }
}