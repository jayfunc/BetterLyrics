using BetterLyrics.Core.Messages.LayoutChanged;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LayoutPreviewControl : UserControl
{
    public static readonly DependencyProperty LayoutProfileProperty =
        DependencyProperty.Register(
            nameof(LayoutProfile),
            typeof(LayoutProfile),
            typeof(LayoutPreviewControl),
            new PropertyMetadata(null, OnLayoutProfileChanged));

    public static readonly DependencyProperty TargetBoundsProperty =
        DependencyProperty.Register(
            nameof(TargetBounds),
            typeof(AppRect),
            typeof(LayoutPreviewControl),
            new PropertyMetadata(null, OnTargetBoundsChanged));

    public LayoutPreviewControl()
    {
        InitializeComponent();
    }

    public LayoutProfile LayoutProfile
    {
        get => (LayoutProfile)GetValue(LayoutProfileProperty);
        set => SetValue(LayoutProfileProperty, value);
    }

    public AppRect TargetBounds
    {
        get => (AppRect)GetValue(TargetBoundsProperty);
        set => SetValue(TargetBoundsProperty, value);
    }

    private static void OnLayoutProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutPreviewControl control) control.RenderPreview();
    }

    private static void OnTargetBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutPreviewControl control)
        {
            var oldBounds = e.OldValue as AppRect;
            var newBounds = e.NewValue as AppRect;
            // 如果新旧 bounds 存在，且宽度与高度均未发生改变（仅坐标变化，如拖拽窗口），则忽略重绘
            if (oldBounds != null && newBounds != null &&
                oldBounds.Width == newBounds.Width &&
                oldBounds.Height == newBounds.Height)
            {
                return;
            }
            control.RenderPreview();
        }
    }

    private void UpdateAspectRatio()
    {
        double previewBaseWidth = 200; // 设定预览图的固定宽度

        if (TargetBounds != null && TargetBounds.Width > 0 && TargetBounds.Height > 0)
        {
            var ratio = TargetBounds.Height / TargetBounds.Width;
            PreviewGrid.Width = previewBaseWidth;
            PreviewGrid.Height = previewBaseWidth * ratio; // 根据主窗口比例算出高度
        }
        else
        {
            PreviewGrid.Width = previewBaseWidth;
            PreviewGrid.Height = previewBaseWidth * 0.75; // 如果没获取到，默认 4:3 比例
        }
    }

    public void RenderPreview()
    {
        if (LayoutProfile == null) return;

        UpdateAspectRatio();

        var scale = 1.0;
        if (TargetBounds != null && TargetBounds.Width > 0)
        {
            scale = PreviewGrid.Width / TargetBounds.Width;
        }
        else
        {
            scale = PreviewGrid.Width / 1280.0; // 假定一个默认的 1280 窗口宽度作为参照
        }

        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        if (LayoutProfile.Placements.Count == 0 && EmptyStateText != null)
        {
            PreviewGrid.Children.Add(EmptyStateText);
            EmptyStateText.Visibility = Visibility.Visible;
            return;
        }

        PreviewGrid.RowSpacing = LayoutProfile.RowSpacing * scale;
        PreviewGrid.ColumnSpacing = LayoutProfile.ColumnSpacing * scale;
        PreviewGrid.Padding = new Thickness(
            LayoutProfile.PaddingLeft * scale,
            LayoutProfile.PaddingTop * scale,
            LayoutProfile.PaddingRight * scale,
            LayoutProfile.PaddingBottom * scale);

        foreach (var rowDef in LayoutProfile.RowDefinitions)
        {
            PreviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLengthExtensions.ParseGridLength(rowDef, scale) });
        }

        foreach (var colDef in LayoutProfile.ColumnDefinitions)
        {
            PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthExtensions.ParseGridLength(colDef, scale) });
        }

        foreach (var placement in LayoutProfile.Placements.OrderBy(x => x.ComponentType))
        {
            var componentVisual = CreateReadOnlyComponentVisual(placement, scale);

            Grid.SetRow(componentVisual, placement.Row);
            Grid.SetColumn(componentVisual, placement.Column);
            Grid.SetRowSpan(componentVisual, placement.RowSpan);
            Grid.SetColumnSpan(componentVisual, placement.ColumnSpan);

            PreviewGrid.Children.Add(componentVisual);
        }
    }

    private Grid CreateReadOnlyComponentVisual(ComponentPlacement placement, double scale)
    {
        var container = new Grid
        {
            Margin = new Thickness(
                placement.MarginLeft * scale,
                placement.MarginTop * scale,
                placement.MarginRight * scale,
                placement.MarginBottom * scale),
            Padding = new Thickness(
                placement.PaddingLeft * scale,
                placement.PaddingTop * scale,
                placement.PaddingRight * scale,
                placement.PaddingBottom * scale),
            HorizontalAlignment = HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment),
            VerticalAlignment = VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment)
        };

        ToolTipService.SetToolTip(container, placement.DisplayName);

        container.Children.Add(MockupHelper.GenerateMockupContent(this, placement.ComponentType,
            HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment),
            placement.DisplayName));

        return container;
    }
}