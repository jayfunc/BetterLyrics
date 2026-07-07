using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class InAppNotificationStack : UserControl
{
    private readonly Dictionary<NotificationItem, FrameworkElement> _itemContainerMap = new();
    private bool _isHovered;

    public InAppNotificationStack()
    {
        InitializeComponent();
    }

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    public void Show(string title, string? message, InfoBarSeverity severity = InfoBarSeverity.Informational,
        TimeSpan? duration = null, bool isClosable = true)
    {
        var item = new NotificationItem
        {
            Title = title,
            Message = message,
            Severity = severity,
            Duration = duration,
            IsClosable = isClosable
        };

        item.OnCloseRequest = i => RemoveItem(i);
        Notifications.Add(item);

        if (duration > TimeSpan.Zero) _ = HandleAutoCloseAsync(item, duration.Value);
    }

    private async Task HandleAutoCloseAsync(NotificationItem item, TimeSpan duration)
    {
        try
        {
            await Task.Delay(duration);
            DispatcherQueue.TryEnqueue(() =>
            {
                if (Notifications.Contains(item)) RemoveItem(item);
            });
        }
        catch
        {
        }
    }

    private async void RemoveItem(NotificationItem item)
    {
        if (!Notifications.Contains(item) || item.IsRemoving) return;

        item.IsRemoving = true;

        if (_itemContainerMap.TryGetValue(item, out var container)) await PlayExitAnimationAsync(container);

        if (Notifications.Contains(item))
        {
            Notifications.Remove(item);
            _itemContainerMap.Remove(item);
        }
    }

    private Task PlayExitAnimationAsync(FrameworkElement element)
    {
        var tcs = new TaskCompletionSource<bool>();

        var sb = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(200);

        var opacityAnim = new DoubleAnimation
        {
            To = 0,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(opacityAnim, element);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");
        sb.Children.Add(opacityAnim);

        var transform = element.RenderTransform as TranslateTransform;
        if (transform == null)
            if (element is Grid g && g.RenderTransform is TranslateTransform gridTrans)
                transform = gridTrans;

        if (transform != null)
        {
            var moveAnim = new DoubleAnimation
            {
                To = transform.Y + 20,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(moveAnim, transform);
            Storyboard.SetTargetProperty(moveAnim, "Y");
            sb.Children.Add(moveAnim);
        }

        sb.Completed += (s, e) => tcs.TrySetResult(true);

        sb.Begin();

        return tcs.Task;
    }

    private void OnInfoBarCloseButtonClick(InfoBar sender, object args)
    {
        if (sender.DataContext is NotificationItem item) RemoveItem(item);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = true;
        UpdateLayoutPositions();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = false;
        UpdateLayoutPositions();
    }

    private void UpdateLayoutPositions()
    {
        double currentY = 0;
        var activeItems = Notifications.Where(i => !i.IsRemoving).ToList();

        for (var i = activeItems.Count - 1; i >= 0; i--)
        {
            var item = activeItems[i];
            if (_itemContainerMap.TryGetValue(item, out var element))
            {
                var group = element.RenderTransform as TransformGroup;
                var scaleTransform = group?.Children.OfType<ScaleTransform>().FirstOrDefault();
                var translateTransform = group?.Children.OfType<TranslateTransform>().FirstOrDefault();

                if (translateTransform != null && scaleTransform != null)
                {
                    double targetY = 0;
                    double targetZ = 0;
                    double targetScale = 1;
                    var itemHeight = element.ActualHeight > 0 ? element.ActualHeight : 68;

                    if (_isHovered)
                    {
                        targetY = currentY;
                        targetZ = 32;
                        targetScale = 1.0;

                        currentY -= itemHeight + 10;
                    }
                    else
                    {
                        var stackIndex = activeItems.Count - 1 - i;
                        var visibleStackLimit = 3;
                        var visualIndex = Math.Min(stackIndex, visibleStackLimit);

                        targetY = -(visualIndex * 12);

                        targetZ = Math.Max(0, 32 - visualIndex * 8);

                        targetScale = 1.0 - visualIndex * 0.04;
                    }

                    AnimateTo(translateTransform, scaleTransform, element, targetY, targetZ, targetScale);
                }
            }
        }
    }

    private void AnimateTo(TranslateTransform trans, ScaleTransform scale, FrameworkElement element, double targetY,
        double targetZ, double targetScale)
    {
        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(300));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var animY = new DoubleAnimation { To = targetY, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animY, trans);
        Storyboard.SetTargetProperty(animY, "Y");
        sb.Children.Add(animY);

        var animScaleX = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animScaleX, scale);
        Storyboard.SetTargetProperty(animScaleX, "ScaleX");
        sb.Children.Add(animScaleX);

        var animScaleY = new DoubleAnimation { To = targetScale, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animScaleY, scale);
        Storyboard.SetTargetProperty(animScaleY, "ScaleY");
        sb.Children.Add(animScaleY);

        var receiver = (element as Grid).Children[1] as Grid;
        var currentTrans = receiver.Translation;
        receiver.Translation = new Vector3(currentTrans.X, currentTrans.Y, (float)targetZ);

        sb.Begin();
    }

    private void OnItemContainerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Grid rootGrid && rootGrid.DataContext is NotificationItem item)
        {
            var infoBar = rootGrid.Children[0] as InfoBar;
            var receiver = rootGrid.Children[1];

            if (receiver != null && infoBar != null && infoBar.Shadow is ThemeShadow themeShadow)
                themeShadow.Receivers.Add(receiver);

            rootGrid.RenderTransformOrigin = new Point(0.5, 1.0);
            _itemContainerMap[item] = rootGrid;
            UpdateLayoutPositions();
        }
    }
}