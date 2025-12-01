using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Controls
{
    public static class InstantTip
    {
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.RegisterAttached("Content", typeof(object), typeof(InstantTip), new PropertyMetadata(null, OnContentChanged));

        public static void SetContent(DependencyObject element, object value) => element.SetValue(ContentProperty, value);
        public static object GetContent(DependencyObject element) => element.GetValue(ContentProperty);

        private static Dictionary<int, Popup> _activePopups = [];

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.PointerEntered -= Element_PointerEntered;
                element.PointerExited -= Element_PointerExited;
                element.Unloaded -= Element_Unloaded;

                if (e.NewValue != null)
                {
                    element.PointerEntered += Element_PointerEntered;
                    element.PointerExited += Element_PointerExited;
                    element.Unloaded += Element_Unloaded;
                }
            }
        }

        private static void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                int hashCode = element.GetHashCode();

                if (_activePopups.ContainsKey(hashCode))
                {
                    _activePopups.TryGetValue(hashCode, out var popup);
                    if (popup != null)
                    {
                        popup.IsOpen = true;
                    }
                }
                else
                {
                    var rawContent = GetContent(element);

                    if (rawContent == null) return;

                    // 创建可视卡片容器 (Visual Card)
                    var visualCard = new Grid
                    {
                        Background = (Brush)Application.Current.Resources["AcrylicBackgroundFillColorDefaultBrush"],
                        CornerRadius = new CornerRadius(4),
                        Shadow = new ThemeShadow(),
                        Translation = new Vector3(0, 0, 32),
                        Padding = new Thickness(8, 4, 8, 4)
                    };

                    var popupContent = new Grid
                    {
                        IsHitTestVisible = false,
                        Opacity = 0,
                        Padding = new Thickness(16)
                    };
                    popupContent.Children.Add(visualCard);

                    var popup = new Popup
                    {
                        Child = popupContent,
                        IsHitTestVisible = false,
                        ShouldConstrainToRootBounds = false,
                        XamlRoot = element.XamlRoot,
                    };

                    object finalContent = rawContent;

                    if (rawContent is ToolTip toolTipWrapper)
                    {
                        finalContent = toolTipWrapper.Content;
                    }

                    if (finalContent is string text)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = text,
                            FontSize = 12,
                            MaxWidth = 320,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                        };
                        visualCard.Children.Add(textBlock);
                    }
                    else if (finalContent != null)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = finalContent.ToString(),
                            FontSize = 12,
                            MaxWidth = 320,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                        };
                        visualCard.Children.Add(textBlock);
                    }

                    var transform = element.TransformToVisual(null);
                    var point = transform.TransformPoint(new Point(0, element.ActualHeight));

                    popup.VerticalOffset = point.Y;
                    popup.HorizontalOffset = point.X - popupContent.Padding.Left;

                    popup.IsOpen = true;

                    App.Current.Resources.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        popupContent.OpacityTransition = new ScalarTransition();
                        popupContent.Opacity = 1;
                    });

                    _activePopups.Add(hashCode, popup);
                }
            }
        }

        private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element != null)
            {
                int hashCode = element.GetHashCode();

                if (!_activePopups.ContainsKey(hashCode))
                {
                    return;
                }

                _activePopups.TryGetValue(hashCode, out var popup);
                if (popup != null)
                {
                    if (popup.Child is Grid popupContent)
                    {
                        popupContent.Opacity = 0;
                    }
                    popup.IsOpen = false;
                    _activePopups.Remove(hashCode);
                }
            }
        }

        private static void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.PointerEntered -= Element_PointerEntered;
                element.PointerExited -= Element_PointerExited;
                element.Unloaded -= Element_Unloaded;
            }
        }
    }
}