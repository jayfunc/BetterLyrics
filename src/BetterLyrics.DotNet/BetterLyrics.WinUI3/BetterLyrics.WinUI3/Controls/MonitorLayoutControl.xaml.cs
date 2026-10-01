using System;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class MonitorLayoutControl : UserControl
{
    private readonly IMonitorProvider _monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();

    public MonitorLayoutControl()
    {
        InitializeComponent();
        Loaded += MonitorLayoutControl_Loaded;
        Unloaded += MonitorLayoutControl_Unloaded;
        SizeChanged += MonitorLayoutControl_SizeChanged;
    }

    private void MonitorLayoutControl_Loaded(object sender, RoutedEventArgs e)
    {
        _monitorProvider.MonitorsChanged += MonitorProvider_MonitorsChanged;
        RenderMonitors();
    }

    private void MonitorLayoutControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _monitorProvider.MonitorsChanged -= MonitorProvider_MonitorsChanged;
    }

    private void MonitorProvider_MonitorsChanged(object? sender, EventArgs e)
    {
        // Wait a bit to ensure Windows display topology is fully updated
        Task.Run(async () =>
        {
            await Task.Delay(500);
            DispatcherQueue.TryEnqueue(() =>
            {
                RenderMonitors();
            });
        });
    }

    private void MonitorLayoutControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderMonitors();
    }

    private void RenderMonitors()
    {
        MonitorCanvas.Children.Clear();

        var availableWidth = ActualWidth;
        if (availableWidth <= 0)
            return;

        var monitorProvider = Ioc.Default.GetRequiredService<IMonitorProvider>();
        var deviceNames = monitorProvider.GetAllMonitorDeviceNames().ToList();

        if (deviceNames.Count == 0)
            return;

        var primaryDeviceName = monitorProvider.GetPrimaryMonitorDeviceName();

        var monitors = deviceNames.Select(name =>
        {
            var rect = monitorProvider.GetMonitorRectFromDeviceName(name);
            return new
            {
                Name = name,
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
                IsPrimary = name == primaryDeviceName
            };
        }).ToList();

        var minX = monitors.Min(m => m.X);
        var minY = monitors.Min(m => m.Y);
        var maxX = monitors.Max(m => m.X + m.Width);
        var maxY = monitors.Max(m => m.Y + m.Height);

        var totalWidth = maxX - minX;
        var totalHeight = maxY - minY;

        if (totalWidth <= 0 || totalHeight <= 0)
            return;

        // 根据控件实际可用宽度计算缩放，限制最大高度为 120
        var maxHeight = 120.0;
        var scaleX = (availableWidth - 48) / totalWidth; // 左右留 24 边距
        var scaleY = maxHeight / totalHeight;
        var scale = Math.Min(scaleX, scaleY);

        var canvasWidth = totalWidth * scale;
        var canvasHeight = totalHeight * scale;

        MonitorCanvas.Width = canvasWidth;
        MonitorCanvas.Height = canvasHeight;

        var groupedMonitors = monitors.GroupBy(m => new { m.X, m.Y, m.Width, m.Height }).ToList();

        foreach (var group in groupedMonitors)
        {
            var rectWidth = group.Key.Width * scale - 2;
            var rectHeight = group.Key.Height * scale - 2;

            if (rectWidth <= 0 || rectHeight <= 0)
                continue;

            var isPrimary = group.Any(m => m.IsPrimary);
            var displayName = string.Join("\n", group.Select(m => m.Name));

            var rect = new Rectangle
            {
                Width = rectWidth,
                Height = rectHeight,
                Fill = isPrimary
                    ? (Brush)Application.Current.Resources["AccentAcrylicBackgroundFillColorDefaultBrush"]
                    : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                Stroke = (Brush)Application.Current.Resources["ControlElevationBorderBrush"],
                StrokeThickness = 1,
                RadiusX = 4,
                RadiusY = 4
            };
            ToolTipService.SetToolTip(rect, displayName);

            var x = (group.Key.X - minX) * scale + 1;
            var y = (group.Key.Y - minY) * scale + 1;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);

            MonitorCanvas.Children.Add(rect);

            var textBlock = new TextBlock
            {
                Text = displayName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = isPrimary
                    ? new SolidColorBrush(Colors.White)
                    : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap
            };
            ToolTipService.SetToolTip(textBlock, displayName);

            var textContainer = new Grid
            {
                Width = rectWidth,
                Height = rectHeight,
                Padding = new Thickness(4),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            ToolTipService.SetToolTip(textContainer, displayName);
            textContainer.Children.Add(textBlock);

            Canvas.SetLeft(textContainer, x);
            Canvas.SetTop(textContainer, y);

            MonitorCanvas.Children.Add(textContainer);
        }
    }
}
