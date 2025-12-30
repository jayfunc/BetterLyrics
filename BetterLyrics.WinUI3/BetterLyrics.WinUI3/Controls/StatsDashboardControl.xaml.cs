using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using BetterLyrics.WinUI3.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StatsDashboardControl : UserControl
{
    public StatsDashboardControlViewModel ViewModel => (StatsDashboardControlViewModel)DataContext;

    public StatsDashboardControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<StatsDashboardControlViewModel>();
        this.Loaded += StatsDashboardControl_Loaded;
    }

    private async void StatsDashboardControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync(StatsRange.Day);
    }

    private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;

        if (TimeRangePivot.SelectedItem is PivotItem item && item.Tag is string tag)
        {
            var range = tag switch
            {
                "Day" => StatsRange.Day,
                "Week" => StatsRange.Week,
                "Month" => StatsRange.Month,
                "Quarter" => StatsRange.Quarter,
                "Year" => StatsRange.Year,
                _ => StatsRange.Day
            };
            await ViewModel.LoadDataAsync(range);
        }
    }
}
