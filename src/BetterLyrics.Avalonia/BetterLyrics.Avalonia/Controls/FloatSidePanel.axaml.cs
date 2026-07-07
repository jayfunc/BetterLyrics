using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BetterLyrics.Core.Enums;
using System;
using System.Reactive.Linq;

namespace BetterLyrics.Avalonia.Controls;

public partial class FloatSidePanel : UserControl
{
    public static readonly StyledProperty<object?> PanelContentProperty =
        AvaloniaProperty.Register<FloatSidePanel, object?>(nameof(PanelContent));

    public static readonly StyledProperty<SidePanelPlacement> PlacementProperty =
        AvaloniaProperty.Register<FloatSidePanel, SidePanelPlacement>(nameof(Placement), SidePanelPlacement.Bottom);

    public object? PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public SidePanelPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public FloatSidePanel()
    {
        InitializeComponent();
        this.GetPropertyChangedObservable(PlacementProperty).Subscribe(new System.Reactive.AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ =>
        {
            UpdatePlacement();
        }));
    }

    private void UpdatePlacement()
    {
        PanelBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
        PanelBorder.VerticalAlignment = VerticalAlignment.Stretch;

        switch (Placement)
        {
            case SidePanelPlacement.Right:
                PanelBorder.HorizontalAlignment = HorizontalAlignment.Right;
                PanelBorder.BorderThickness = new Thickness(1, 0, 0, 0);
                break;
            case SidePanelPlacement.Left:
                PanelBorder.HorizontalAlignment = HorizontalAlignment.Left;
                PanelBorder.BorderThickness = new Thickness(0, 0, 1, 0);
                break;
            case SidePanelPlacement.Top:
                PanelBorder.VerticalAlignment = VerticalAlignment.Top;
                PanelBorder.BorderThickness = new Thickness(0, 0, 0, 1);
                break;
            case SidePanelPlacement.Bottom:
                PanelBorder.VerticalAlignment = VerticalAlignment.Bottom;
                PanelBorder.BorderThickness = new Thickness(0, 1, 0, 0);
                break;
        }
    }

    public async void Show()
    {
        RootContainer.IsVisible = true;
        // 强制布局更新以获取 ActualWidth/Height
        await System.Threading.Tasks.Task.Delay(10);

        var width = PanelBorder.Bounds.Width;
        var height = PanelBorder.Bounds.Height;

        var panelTranslateTransform = (TranslateTransform?)PanelBorder.RenderTransform;

        // 设置动画起始位置（隐藏状态）
        switch (Placement)
        {
            case SidePanelPlacement.Right: panelTranslateTransform?.X = width; break;
            case SidePanelPlacement.Left: panelTranslateTransform?.X = -width; break;
            case SidePanelPlacement.Bottom: panelTranslateTransform?.Y = height; break;
            case SidePanelPlacement.Top: panelTranslateTransform?.Y = -height; break;
        }

        // 执行进入动画
        MaskBorder.Opacity = 1;
        panelTranslateTransform?.X = 0;
        panelTranslateTransform?.Y = 0;
    }

    public async void Hide()
    {
        var width = PanelBorder.Bounds.Width;
        var height = PanelBorder.Bounds.Height;

        var panelTranslateTransform = (TranslateTransform?)PanelBorder.RenderTransform;

        // 设置动画结束位置（隐藏状态）
        switch (Placement)
        {
            case SidePanelPlacement.Right: panelTranslateTransform?.X = width; break;
            case SidePanelPlacement.Left: panelTranslateTransform?.X = -width; break;
            case SidePanelPlacement.Bottom: panelTranslateTransform?.Y = height; break;
            case SidePanelPlacement.Top: panelTranslateTransform?.Y = -height; break;
        }

        MaskBorder.Opacity = 0;

        // 等待动画结束
        await System.Threading.Tasks.Task.Delay(350);
        RootContainer.IsVisible = false;
    }

    private void Mask_Tapped(object? sender, PointerPressedEventArgs e) => Hide();
}