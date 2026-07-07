using System;
using System.Globalization;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StatsDashboardControl : UserControl
{
    public StatsDashboardControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<StatsDashboardControlViewModel>();
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
}