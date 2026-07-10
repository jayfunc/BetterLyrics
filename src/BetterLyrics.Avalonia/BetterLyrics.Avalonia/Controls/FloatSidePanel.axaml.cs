using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Media.Transformation;
using global::Avalonia.Metadata;
using BetterLyrics.Core.Enums;
using System;
using System.Reactive.Linq;

namespace BetterLyrics.Avalonia.Controls;

public partial class FloatSidePanel : UserControl
{
    public static readonly StyledProperty<SidePanelPlacement> PlacementProperty =
        AvaloniaProperty.Register<FloatSidePanel, SidePanelPlacement>(nameof(Placement), SidePanelPlacement.Bottom);

    public SidePanelPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public FloatSidePanel()
    {
        InitializeComponent();
        DataContext = this;
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
        if (panelTranslateTransform == null) return;

        // 临时移除过渡效果，瞬间定位到初始隐藏位置
        var transitions = panelTranslateTransform.Transitions;
        panelTranslateTransform.Transitions = null;

        // 设置动画起始位置（隐藏状态）
        switch (Placement)
        {
            case SidePanelPlacement.Right: panelTranslateTransform.X = width; break;
            case SidePanelPlacement.Left: panelTranslateTransform.X = -width; break;
            case SidePanelPlacement.Bottom: panelTranslateTransform.Y = height; break;
            case SidePanelPlacement.Top: panelTranslateTransform.Y = -height; break;
        }

        // 等待布局生效
        await System.Threading.Tasks.Task.Delay(10);
        panelTranslateTransform.Transitions = transitions;

        // 执行进入动画
        MaskBorder.Opacity = 1;
        switch (Placement)
        {
            case SidePanelPlacement.Right:
            case SidePanelPlacement.Left:
                panelTranslateTransform.X = 0;
                break;
            case SidePanelPlacement.Bottom:
            case SidePanelPlacement.Top:
                panelTranslateTransform.Y = 0;
                break;
        }
    }

    public async void Hide()
    {
        var width = PanelBorder.Bounds.Width;
        var height = PanelBorder.Bounds.Height;

        var panelTranslateTransform = (TranslateTransform?)PanelBorder.RenderTransform;
        if (panelTranslateTransform == null) return;

        // 设置动画结束位置（隐藏状态）
        switch (Placement)
        {
            case SidePanelPlacement.Right: panelTranslateTransform.X = width; break;
            case SidePanelPlacement.Left: panelTranslateTransform.X = -width; break;
            case SidePanelPlacement.Bottom: panelTranslateTransform.Y = height; break;
            case SidePanelPlacement.Top: panelTranslateTransform.Y = -height; break;
        }

        MaskBorder.Opacity = 0;

        // 等待动画结束
        await System.Threading.Tasks.Task.Delay(350);
        RootContainer.IsVisible = false;
    }

    private void Mask_Tapped(object? sender, PointerPressedEventArgs e) => Hide();
}