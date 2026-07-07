using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ExternalLinkButton : HyperlinkButton
{
    private Storyboard? _hoverStoryboard;
    private FrameworkElement? _originalContent;
    private Grid? _overlayGrid;

    public ExternalLinkButton()
    {
        Loaded += ExternalLinkButton_Loaded;
        PointerEntered += ExternalLinkButton_PointerEntered;
        PointerExited += ExternalLinkButton_PointerExited;
    }

    private void ExternalLinkButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateContent();
    }

    private void ExternalLinkButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        AnimateHoverState(1.0, 0.3);
    }

    private void ExternalLinkButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        AnimateHoverState(0.0, 1.0);
    }

    private void AnimateHoverState(double overlayOpacity, double contentOpacity)
    {
        if (_overlayGrid == null || _originalContent == null) return;

        _hoverStoryboard = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(200));
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        var overlayAnim = new DoubleAnimation { To = overlayOpacity, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(overlayAnim, _overlayGrid);
        Storyboard.SetTargetProperty(overlayAnim, "Opacity");

        var opacityAnim = new DoubleAnimation { To = contentOpacity, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(opacityAnim, _originalContent);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");

        _hoverStoryboard.Children.Add(overlayAnim);
        _hoverStoryboard.Children.Add(opacityAnim);

        _hoverStoryboard.Begin();
    }

    private void UpdateContent()
    {
        FrameworkElement? element = null;
        if (Content is string textContent)
        {
            element = new TextBlock
            {
                Text = textContent,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else if (Content is FrameworkElement frameworkElement)
        {
            element = frameworkElement;
            if (element.Tag?.ToString() == "ExternalLinkButtonRoot")
            {
                if (element is Grid rootGrid)
                {
                    _originalContent =
                        rootGrid.Children.FirstOrDefault(c =>
                            c is FrameworkElement fe && fe.Tag?.ToString() == "OriginalContent") as FrameworkElement;
                    _overlayGrid =
                        rootGrid.Children.FirstOrDefault(c =>
                            c is Grid g && g.Tag?.ToString() == "OverlayGrid") as Grid;
                }

                return;
            }
        }

        if (element == null) return;

        var isHovered = IsPointerOver;

        var rootPanel = new Grid { Tag = "ExternalLinkButtonRoot" };

        _originalContent = element;
        _originalContent.Tag = "OriginalContent";
        _originalContent.HorizontalAlignment = HorizontalAlignment.Center;
        _originalContent.VerticalAlignment = VerticalAlignment.Center;
        _originalContent.Opacity = isHovered ? 0.3 : 1.0;

        rootPanel.Children.Add(_originalContent);

        _overlayGrid = new Grid
        {
            Tag = "OverlayGrid",
            Opacity = isHovered ? 1.0 : 0.0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var icon = new FontIcon
        {
            FontFamily = (FontFamily)Application.Current.Resources["SegoeFluentIcons"],
            Glyph = "\uE8A7",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _overlayGrid.Children.Add(icon);
        rootPanel.Children.Add(_overlayGrid);

        Content = rootPanel;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        UpdateContent();
    }
}