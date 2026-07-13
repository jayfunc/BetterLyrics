using System.Linq;
using Windows.Foundation;
using BetterLyrics.Core.Messages;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LayoutPreviewControl : UserControl,
    IRecipient<PropertyChangedMessage<Rect>>,
    IRecipient<LayoutChangedMessage>
{
    public static readonly DependencyProperty LayoutProfileProperty =
        DependencyProperty.Register(
            nameof(LayoutProfile),
            typeof(LayoutProfile),
            typeof(LayoutPreviewControl),
            new PropertyMetadata(null, OnLayoutProfileChanged));

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(
            nameof(LyricsWindowStatus),
            typeof(LyricsWindowStatus),
            typeof(LayoutPreviewControl),
            new PropertyMetadata(null, OnLyricsWindowStatusChanged));

    public LayoutPreviewControl()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public LayoutProfile LayoutProfile
    {
        get => (LayoutProfile)GetValue(LayoutProfileProperty);
        set => SetValue(LayoutProfileProperty, value);
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public void Receive(LayoutChangedMessage message)
    {
        RenderPreview();
    }

    public void Receive(PropertyChangedMessage<Rect> message)
    {
        if (LyricsWindowStatus != null && message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.WindowBounds))
                RenderPreview();
    }

    private static void OnLayoutProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutPreviewControl control) control.RenderPreview();
    }

    private static void OnLyricsWindowStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutPreviewControl control) control.RenderPreview();
    }

    private void UpdateAspectRatio()
    {
        double previewBaseWidth = 200; // 设定预览图的固定宽度

        if (LyricsWindowStatus != null && LyricsWindowStatus.WindowBounds.Width > 0 &&
            LyricsWindowStatus.WindowBounds.Height > 0)
        {
            var ratio = LyricsWindowStatus.WindowBounds.Height / LyricsWindowStatus.WindowBounds.Width;
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
        if (LyricsWindowStatus != null && LyricsWindowStatus.WindowBounds.Width > 0)
            scale = PreviewGrid.Width / LyricsWindowStatus.WindowBounds.Width;
        else
            scale = PreviewGrid.Width / 1280.0; // 假定一个默认的 1280 窗口宽度作为参照

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
            PreviewGrid.RowDefinitions.Add(new RowDefinition
                { Height = GridLengthExtensions.ParseGridLength(rowDef, scale) });

        foreach (var colDef in LayoutProfile.ColumnDefinitions)
            PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLengthExtensions.ParseGridLength(colDef, scale) });

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
            HorizontalAlignment =
                HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment),
            VerticalAlignment = VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment)
        };

        ToolTipService.SetToolTip(container, placement.DisplayName);

        container.Children.Add(MockupHelper.GenerateMockupContent(this, placement.ComponentType,
            HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment),
            placement.DisplayName));

        return container;
    }
}