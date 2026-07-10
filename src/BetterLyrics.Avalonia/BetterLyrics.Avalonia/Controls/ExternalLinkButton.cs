using System;
using System.Diagnostics;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Animation;
using global::Avalonia.Interactivity;

namespace BetterLyrics.Avalonia.Controls;

public partial class ExternalLinkButton : HyperlinkButton
{
    private Control? _originalContent;
    private Grid? _overlayGrid;

    // 防止属性更改触发递归的锁
    private bool _isUpdatingContent = false;

    // 如果基类已经有 NavigateUri (如 FluentAvalonia)，new 关键字会隐藏基类属性
    public static new readonly StyledProperty<string?> NavigateUriProperty =
        AvaloniaProperty.Register<ExternalLinkButton, string?>(nameof(NavigateUri));

    public new string? NavigateUri
    {
        get => GetValue(NavigateUriProperty);
        set => SetValue(NavigateUriProperty, value);
    }

    public ExternalLinkButton()
    {
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(8,4);
        Cursor = new Cursor(StandardCursorType.Hand);

        Loaded += ExternalLinkButton_Loaded;
        PointerEntered += ExternalLinkButton_PointerEntered;
        PointerExited += ExternalLinkButton_PointerExited;
    }

    protected override void OnClick()
    {
        base.OnClick();
        if (!string.IsNullOrEmpty(NavigateUri))
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = NavigateUri,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            catch { /* 处理无法打开链接的异常 */ }
        }
    }

    private void ExternalLinkButton_Loaded(object? sender, RoutedEventArgs e)
    {
        UpdateContent();
    }

    private void ExternalLinkButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        SetHoverState(1.0, 0.3);
    }

    private void ExternalLinkButton_PointerExited(object? sender, PointerEventArgs e)
    {
        SetHoverState(0.0, 1.0);
    }

    private void SetHoverState(double overlayOpacity, double contentOpacity)
    {
        if (_overlayGrid != null)
            _overlayGrid.Opacity = overlayOpacity;

        if (_originalContent != null)
            _originalContent.Opacity = contentOpacity;
    }

    private void UpdateContent()
    {
        // 1. 如果正在更新中，或者是已经包装好的 Grid，则跳过
        if (_isUpdatingContent) return;

        object? currentContent = Content;
        if (currentContent is Control { Tag: "ExternalLinkButtonRoot" }) return;

        // 加锁防止递归
        _isUpdatingContent = true;

        try
        {
            // 2. 【核心修复】：必须先将 Content 置空，切断原控件与 Button 的父子关系
            // 否则直接将其添加到 Grid 中会报 "Control already has a visual parent"
            Content = null;

            Control? element = null;
            if (currentContent is string textContent)
            {
                element = new TextBlock
                {
                    Text = textContent,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else if (currentContent is Control control)
            {
                element = control;
            }

            if (element == null) return;

            var isHovered = IsPointerOver;
            var rootPanel = new Grid { Tag = "ExternalLinkButtonRoot" };

            // 3. 处理原内容
            _originalContent = element;
            _originalContent.HorizontalAlignment = HorizontalAlignment.Center;
            _originalContent.VerticalAlignment = VerticalAlignment.Center;
            _originalContent.Opacity = isHovered ? 0.3 : 1.0;

            // Avalonia 原生过渡动画替代 Storyboard
            _originalContent.Transitions = new Transitions
            {
                new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(200) }
            };

            rootPanel.Children.Add(_originalContent);

            // 4. 构建悬浮遮罩层
            _overlayGrid = new Grid
            {
                Tag = "OverlayGrid",
                Opacity = isHovered ? 1.0 : 0.0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Transitions = new Transitions
                {
                    new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(200) }
                }
            };

            var icon = new TextBlock
            {
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                Text = "\uE8A7",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _overlayGrid.Children.Add(icon);
            rootPanel.Children.Add(_overlayGrid);

            // 5. 重新将包装后的容器赋给 Content
            Content = rootPanel;
        }
        finally
        {
            // 解锁
            _isUpdatingContent = false;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 监听 Content 变化时触发重新包装
        if (change.Property == ContentProperty && change.OldValue != change.NewValue)
        {
            UpdateContent();
        }
    }
}